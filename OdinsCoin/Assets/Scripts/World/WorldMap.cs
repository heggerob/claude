using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The real world, from Iceland to the Baltic and from the English Channel to the North Cape: land height and
    /// sea depth in metres at any point, and the way between latitude/longitude and the game's flat world
    /// (x east, z north, 1 unit = 1 metre times <see cref="Scale"/>).
    /// Built by tools/world/build_map.py from real elevation data (AWS Terrain Tiles) into
    /// Resources/World/north.bytes. Latitude/longitude are projected sinusoidally about <see cref="Lon0"/>,
    /// <see cref="Lat0"/>, which keeps distances along the parallels and the meridians true; the maths matches the
    /// build script's, so change both together.
    /// </summary>
    public class WorldMap
    {
        public const float EarthRadius = 6371000f;

        /// <summary>How big the world is: 1 is full size (a real sea mile is a sea mile).</summary>
        public static float Scale = 1f;

        public int Width { get; private set; }
        public int Height { get; private set; }
        /// <summary>Grid spacing in metres (real, before <see cref="Scale"/>).</summary>
        public float Cell { get; private set; }
        /// <summary>The south-west corner of the grid, in real metres.</summary>
        public float OriginX { get; private set; }
        public float OriginZ { get; private set; }
        public float Lon0 { get; private set; }
        public float Lat0 { get; private set; }

        short[] heights;

        /// <summary>The fine coast layer (200 m, where there is coast), or null.</summary>
        public WorldDetail Detail { get; set; }

        static WorldMap current;

        /// <summary>The game's map, loaded from Resources the first time it's asked for (null if it's missing).</summary>
        public static WorldMap Current
        {
            get
            {
                if (current == null)
                {
                    var asset = Resources.Load<TextAsset>("World/north");
                    if (asset != null && asset.bytes != null && asset.bytes.Length > 0)
                    {
                        current = FromBytes(asset.bytes);
                        var coast = Resources.Load<TextAsset>("World/coast");
                        if (coast != null && coast.bytes != null && coast.bytes.Length > 0) current.Detail = WorldDetail.FromBytes(coast.bytes);
                    }
                }
                return current;
            }
            set { current = value; }
        }

        /// <summary>Reads a map file: "OCWM", version, width, height, cell, origin x/z, lon0, lat0, radius, spare, then gzip'd int16 heights (rows south to north).</summary>
        public static WorldMap FromBytes(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms))
            {
                var magic = r.ReadBytes(4);
                if (magic.Length != 4 || magic[0] != 'O' || magic[1] != 'C' || magic[2] != 'W' || magic[3] != 'M') throw new InvalidDataException("not an Odin's Coin world map");
                int version = r.ReadInt32();
                if (version != 1) throw new InvalidDataException("unknown world map version " + version);
                var m = new WorldMap();
                m.Width = r.ReadInt32();
                m.Height = r.ReadInt32();
                m.Cell = r.ReadSingle();
                m.OriginX = r.ReadSingle();
                m.OriginZ = r.ReadSingle();
                m.Lon0 = r.ReadSingle();
                m.Lat0 = r.ReadSingle();
                r.ReadSingle(); // radius (the same as EarthRadius)
                r.ReadSingle(); // spare
                if (m.Width <= 1 || m.Height <= 1 || m.Cell <= 0f) throw new InvalidDataException("bad world map size");
                m.heights = new short[m.Width * m.Height];
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                {
                    var raw = new byte[m.heights.Length * 2];
                    int got = 0;
                    while (got < raw.Length)
                    {
                        int n = gz.Read(raw, got, raw.Length - got);
                        if (n <= 0) throw new InvalidDataException("world map is cut short");
                        got += n;
                    }
                    Buffer.BlockCopy(raw, 0, m.heights, 0, raw.Length);
                }
                return m;
            }
        }

        /// <summary>A map from given heights (for tests): <paramref name="h"/> is width × height, rows south to north.</summary>
        public static WorldMap FromHeights(short[] h, int width, int height, float cell, float originX, float originZ, float lon0, float lat0)
        {
            return new WorldMap { heights = h, Width = width, Height = height, Cell = cell, OriginX = originX, OriginZ = originZ, Lon0 = lon0, Lat0 = lat0 };
        }

        // ---------------------------------------------------------------- projection

        /// <summary>Latitude/longitude (degrees) to game position (x east, z north; y is left at 0).</summary>
        public Vector3 ToWorld(float latitude, float longitude)
        {
            double lat = latitude * Math.PI / 180.0, dLon = (longitude - Lon0) * Math.PI / 180.0;
            double x = EarthRadius * dLon * Math.Cos(lat);
            double z = EarthRadius * (latitude - Lat0) * Math.PI / 180.0;
            return new Vector3((float)x * Scale, 0f, (float)z * Scale);
        }

        /// <summary>Game position to latitude (x) and longitude (y), in degrees.</summary>
        public Vector2 ToLatLon(Vector3 world)
        {
            double z = world.z / Scale, x = world.x / Scale;
            double lat = Lat0 + z / EarthRadius * 180.0 / Math.PI;
            double c = Math.Max(Math.Cos(lat * Math.PI / 180.0), 1e-6);
            double lon = Lon0 + x / (EarthRadius * c) * 180.0 / Math.PI;
            return new Vector2((float)lat, (float)lon);
        }

        // ---------------------------------------------------------------- heights

        /// <summary>
        /// The height of the ground (negative: the sea floor) at a game position, in game units: from the fine
        /// coast layer where there is one, otherwise smoothly (bicubic) from the 1 km map.
        /// </summary>
        public float GroundHeight(float x, float z)
        {
            float rx = x / Scale, rz = z / Scale, fine;
            if (Detail != null && Detail.TryHeight(rx, rz, out fine)) return fine * Scale;
            return Coarse(rx, rz) * Scale;
        }

        /// <summary>The 1 km map alone at a real position (metres), bicubic so it has no creases.</summary>
        public float Coarse(float rx, float rz)
        {
            float gx = Mathf.Clamp((rx - OriginX) / Cell, 0f, Width - 1.001f);
            float gz = Mathf.Clamp((rz - OriginZ) / Cell, 0f, Height - 1.001f);
            int ix = (int)gx, iz = (int)gz;
            float ax = gx - ix, az = gz - iz;
            var rows = new float[4];
            for (int j = -1; j <= 2; j++)
                rows[j + 1] = CatmullRom(At(ix - 1, iz + j), At(ix, iz + j), At(ix + 1, iz + j), At(ix + 2, iz + j), ax);
            return CatmullRom(rows[0], rows[1], rows[2], rows[3], az);
        }

        static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t);
        }

        /// <summary>The raw height of grid cell (ix, iz), in real metres.</summary>
        public float At(int ix, int iz)
        {
            ix = Mathf.Clamp(ix, 0, Width - 1);
            iz = Mathf.Clamp(iz, 0, Height - 1);
            return heights[iz * Width + ix];
        }

        /// <summary>Is there sea (the ground below the waterline) at this game position?</summary>
        public bool IsSea(float x, float z) { return GroundHeight(x, z) < 0f; }

        /// <summary>The world's extent in game units: min corner (x, z) and size.</summary>
        public Rect Bounds { get { return new Rect(OriginX * Scale, OriginZ * Scale, (Width - 1) * Cell * Scale, (Height - 1) * Cell * Scale); } }
    }
}

namespace OdinsCoin
{
    /// <summary>
    /// The fine coast layer: 25 km blocks at 200 m wherever there is coast (built by tools/world/build_detail.py
    /// into Resources/World/coast.bytes), so the fjords, sounds and skerry belts are there. Each block is
    /// compressed on its own and unpacked only when the ship comes near; a few are kept unpacked.
    /// </summary>
    public class WorldDetail
    {
        public float BlockSize { get; private set; }
        public float Cell { get; private set; }
        public int Points { get; private set; }
        public float OriginX { get; private set; }
        public float OriginZ { get; private set; }
        public int BlockCount { get { return index.Count; } }

        /// <summary>How many unpacked blocks are kept (each ~32 KB).</summary>
        public const int Keep = 48;

        byte[] data;
        int dataStart;
        readonly System.Collections.Generic.Dictionary<long, int[]> index = new System.Collections.Generic.Dictionary<long, int[]>();
        readonly System.Collections.Generic.Dictionary<long, short[]> unpacked = new System.Collections.Generic.Dictionary<long, short[]>();
        readonly System.Collections.Generic.LinkedList<long> recent = new System.Collections.Generic.LinkedList<long>();

        static long Key(int i, int j) { return ((long)i << 32) ^ (uint)j; }

        public static WorldDetail FromBytes(byte[] bytes)
        {
            using (var ms = new System.IO.MemoryStream(bytes))
            using (var r = new System.IO.BinaryReader(ms))
            {
                var magic = r.ReadBytes(4);
                if (magic.Length != 4 || magic[0] != 'O' || magic[1] != 'C' || magic[2] != 'W' || magic[3] != 'D') throw new System.IO.InvalidDataException("not an Odin's Coin coast layer");
                if (r.ReadInt32() != 1) throw new System.IO.InvalidDataException("unknown coast layer version");
                var d = new WorldDetail();
                d.BlockSize = r.ReadSingle();
                d.Cell = r.ReadSingle();
                d.Points = r.ReadInt32();
                d.OriginX = r.ReadSingle();
                d.OriginZ = r.ReadSingle();
                int count = r.ReadInt32();
                for (int k = 0; k < count; k++)
                {
                    int bi = r.ReadInt32(), bj = r.ReadInt32(), off = r.ReadInt32(), len = r.ReadInt32();
                    d.index[Key(bi, bj)] = new[] { off, len };
                }
                d.data = bytes;
                d.dataStart = (int)ms.Position;
                return d;
            }
        }

        /// <summary>Is there a fine block at this real position (metres)?</summary>
        public bool Covers(float rx, float rz)
        {
            int bi = (int)System.Math.Floor((rx - OriginX) / BlockSize), bj = (int)System.Math.Floor((rz - OriginZ) / BlockSize);
            return index.ContainsKey(Key(bi, bj));
        }

        /// <summary>The fine height at a real position (metres), if a block covers it.</summary>
        public bool TryHeight(float rx, float rz, out float height)
        {
            height = 0f;
            float lx = rx - OriginX, lz = rz - OriginZ;
            int bi = (int)System.Math.Floor(lx / BlockSize), bj = (int)System.Math.Floor(lz / BlockSize);
            var block = Block(bi, bj);
            if (block == null) return false;
            float gx = Mathf.Clamp((lx - bi * BlockSize) / Cell, 0f, Points - 1.001f);
            float gz = Mathf.Clamp((lz - bj * BlockSize) / Cell, 0f, Points - 1.001f);
            int ix = (int)gx, iz = (int)gz;
            float ax = gx - ix, az = gz - iz;
            int n = Points;
            float h00 = block[iz * n + ix], h10 = block[iz * n + ix + 1], h01 = block[(iz + 1) * n + ix], h11 = block[(iz + 1) * n + ix + 1];
            height = Mathf.Lerp(Mathf.Lerp(h00, h10, ax), Mathf.Lerp(h01, h11, ax), az);
            return true;
        }

        short[] Block(int bi, int bj)
        {
            long key = Key(bi, bj);
            short[] block;
            if (unpacked.TryGetValue(key, out block))
            {
                recent.Remove(key);
                recent.AddFirst(key);
                return block;
            }
            int[] at;
            if (!index.TryGetValue(key, out at)) return null;
            block = new short[Points * Points];
            using (var ms = new System.IO.MemoryStream(data, dataStart + at[0], at[1]))
            using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
            {
                var raw = new byte[block.Length * 2];
                int got = 0;
                while (got < raw.Length)
                {
                    int k = gz.Read(raw, got, raw.Length - got);
                    if (k <= 0) throw new System.IO.InvalidDataException("coast block is cut short");
                    got += k;
                }
                System.Buffer.BlockCopy(raw, 0, block, 0, raw.Length);
            }
            unpacked[key] = block;
            recent.AddFirst(key);
            while (recent.Count > Keep) { unpacked.Remove(recent.Last.Value); recent.RemoveLast(); }
            return block;
        }
    }
}
