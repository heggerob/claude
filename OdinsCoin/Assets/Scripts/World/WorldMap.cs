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

        static WorldMap current;

        /// <summary>The game's map, loaded from Resources the first time it's asked for (null if it's missing).</summary>
        public static WorldMap Current
        {
            get
            {
                if (current == null)
                {
                    var asset = Resources.Load<TextAsset>("World/north");
                    if (asset != null && asset.bytes != null && asset.bytes.Length > 0) current = FromBytes(asset.bytes);
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

        /// <summary>The height of the ground (negative: the sea floor) at a game position, in game units, smoothly interpolated.</summary>
        public float GroundHeight(float x, float z)
        {
            float gx = (x / Scale - OriginX) / Cell, gz = (z / Scale - OriginZ) / Cell;
            gx = Mathf.Clamp(gx, 0f, Width - 1.001f);
            gz = Mathf.Clamp(gz, 0f, Height - 1.001f);
            int ix = (int)gx, iz = (int)gz;
            float ax = gx - ix, az = gz - iz;
            float h00 = At(ix, iz), h10 = At(ix + 1, iz), h01 = At(ix, iz + 1), h11 = At(ix + 1, iz + 1);
            float h = Mathf.Lerp(Mathf.Lerp(h00, h10, ax), Mathf.Lerp(h01, h11, ax), az);
            return h * Scale;
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
