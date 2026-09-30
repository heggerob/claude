using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Finds the maps tucked in plundered chests, and sends a raven to circle over a mapped hoard's cairn once
    /// you're near enough to go and look.
    /// </summary>
    public class HoardGuide : MonoBehaviour
    {
        WorldMap map;
        Transform world, focus;
        readonly Dictionary<string, RavenBird> ravens = new Dictionary<string, RavenBird>();
        float nextCheck;

        public void Setup(WorldMap map, Transform world, Transform focus)
        {
            this.map = map;
            this.world = world;
            this.focus = focus;
        }

        void OnEnable() { TreasureChest.Opened += OnOpened; }
        void OnDisable() { TreasureChest.Opened -= OnOpened; }

        void OnOpened(TreasureChest chest)
        {
            if (map == null || !Hoards.HasMap(Random.value)) return;
            var u = Upgrades.Current;
            var here = new Vector3((float)WorldOrigin.GlobalX(chest.transform.position), 0f, (float)WorldOrigin.GlobalZ(chest.transform.position));
            var to = Hoards.MapTo(map, here, u.Dug, u.Maps);
            if (to == null) return;
            u.Maps.Add(to.name);
            CombatHud.Banner("A TREASURE MAP", "Tucked in the chest: a map to a hoard buried near " + to.name + ". It's marked on your chart (M).");
        }

        void Update()
        {
            if (map == null || focus == null || Time.time < nextCheck) return;
            nextCheck = Time.time + 1f;
            var u = Upgrades.Current;
            // Ravens over the cairns of mapped hoards you're near; gone once they're dug.
            foreach (var kv in Hoards.Cairns)
            {
                if (kv.Key == null) continue;
                bool mapped = u.Maps.Contains(kv.Value.name);
                bool near = Vector3.Distance(kv.Key.position, focus.position) < Hoards.RavenRange;
                RavenBird bird;
                ravens.TryGetValue(kv.Value.name, out bird);
                if (mapped && near && bird == null)
                    ravens[kv.Value.name] = RavenBird.Create(world, kv.Key.position + Vector3.up * 40f, kv.Key, false);
            }
            foreach (var name in new List<string>(ravens.Keys))
                if (!u.Maps.Contains(name) && ravens[name] != null) { ravens[name].FlyAway(); ravens.Remove(name); }
        }
    }
}
