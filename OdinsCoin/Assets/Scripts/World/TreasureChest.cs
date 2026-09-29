using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A wooden chest with iron bands and gold spilling out. Holds a base amount of gold; Freya's Gift and
    /// Fenrir's Hunger change what you get when you cash it in (roadmap item 7 adds carrying it home).
    /// </summary>
    public class TreasureChest : MonoBehaviour
    {
        public int BaseGold;
        public bool Looted;

        public static TreasureChest Create(Transform parent, Vector3 worldPos, float yaw, int gold)
        {
            var go = new GameObject("Treasure Chest");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = new Color(0.48f, 0.3f, 0.16f);
            var iron = new Color(0.3f, 0.3f, 0.32f);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.65f), wood);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.68f, 0f), new Vector3(1.02f, 0.18f, 0.67f), wood * 0.85f);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(-0.3f, 0.4f, 0f), new Vector3(0.08f, 0.82f, 0.69f), iron);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0.3f, 0.4f, 0f), new Vector3(0.08f, 0.82f, 0.69f), iron);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.45f, 0.34f), new Vector3(0.14f, 0.16f, 0.04f), Materials.Gold);
            LongshipBuilder.Deco(PrimitiveType.Sphere, go.transform, new Vector3(0.15f, 0.8f, -0.05f), new Vector3(0.3f, 0.12f, 0.3f), Materials.Gold);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.38f, 0f);
            col.size = new Vector3(1f, 0.76f, 0.65f);
            var chest = go.AddComponent<TreasureChest>();
            chest.BaseGold = gold;
            return chest;
        }

        /// <summary>What this chest is worth right now, after blessings and curses.</summary>
        public int Value { get { return Mathf.RoundToInt(BaseGold * Fortune.Current.LootMultiplier); } }
    }
}
