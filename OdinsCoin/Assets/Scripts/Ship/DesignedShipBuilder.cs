using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Builds one of the new ship classes (<see cref="ShipDesign"/>) as a ship you can sail and walk on: the drawn
    /// hull from <see cref="ShipModel"/>, each sail hung on its own so it braces round and furls, a deck to stand
    /// on and rails to stop you sliding overboard, the helm at the tiller and Odin's altar before the main mast.
    /// </summary>
    public static class DesignedShipBuilder
    {
        /// <summary>The deck's height above the waterline (the planks sit a little below the rail).</summary>
        public static float DeckY(ShipDesign d) { return d.freeboard - 0.52f; }

        /// <summary>Where the helmsman stands (ship-local): at the tiller by the stern, a step to starboard.</summary>
        public static Vector3 HelmStation(ShipDesign d, float deckY)
        {
            float k, g, hb;
            ShipModel.Section(d, (-d.length / 2f + 2.8f) / (d.length / 2f), out k, out g, out hb);
            return new Vector3(Mathf.Min(1.1f, hb * 0.45f), deckY + 0.05f, -d.length / 2f + 2.8f);
        }

        public static LongshipBuilder.Parts Build(Transform root, ShipDesign d, ShipLook look)
        {
            var parts = new LongshipBuilder.Parts { root = root, deck = root };
            float deckY = DeckY(d);

            var hull = new GameObject("Hull").transform;
            hull.SetParent(root, false);
            ModelView.Show(ShipModel.Build(d, look, false), hull);

            // The sails: each turns on its mast (the brace) and gathers up to its yard (the furl).
            parts.rigs = new Transform[d.sails.Length];
            parts.cloths = new Transform[d.sails.Length];
            for (int i = 0; i < d.sails.Length; i++)
            {
                VikingModel yard, cloth;
                Vector3 pivot, gather;
                ShipModel.SailParts(d, i, look, out yard, out cloth, out pivot, out gather);
                var rig = new GameObject(d.sails[i].name + " Sail").transform;
                rig.SetParent(root, false);
                rig.localPosition = pivot;
                var yardView = new GameObject("Yard").transform;
                yardView.SetParent(rig, false);
                yardView.localPosition = -pivot;
                ModelView.Show(yard, yardView);
                var clothPivot = new GameObject("Cloth").transform;
                clothPivot.SetParent(rig, false);
                clothPivot.localPosition = gather - pivot;
                var clothView = new GameObject("Canvas").transform;
                clothView.SetParent(clothPivot, false);
                clothView.localPosition = -gather;
                ModelView.Show(cloth, clothView);
                parts.rigs[i] = rig;
                parts.cloths[i] = clothPivot;
            }

            // The deck: solid blocks down the hull, each as wide as the hull there, their tops the planks.
            const int blocks = 10;
            for (int i = 0; i < blocks; i++)
            {
                float t0 = -0.9f + 1.8f * i / blocks, t1 = -0.9f + 1.8f * (i + 1) / blocks;
                float k, g, hb0, hb1;
                ShipModel.Section(d, t0, out k, out g, out hb0);
                ShipModel.Section(d, t1, out k, out g, out hb1);
                var block = new GameObject("Deck").transform;
                block.SetParent(root, false);
                block.localPosition = new Vector3(0f, deckY - 0.65f, (t0 + t1) / 2f * d.length / 2f);
                var col = block.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(2f * Mathf.Min(hb0, hb1) * 0.9f, 1.3f, (t1 - t0) * d.length / 2f + 0.05f);
            }

            // Invisible rails along the gunwales so you don't slide overboard by accident (you can still jump over).
            const int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float t0 = -0.85f + 1.7f * i / segments, t1 = -0.85f + 1.7f * (i + 1) / segments;
                float k, g, hb0, hb1;
                ShipModel.Section(d, t0, out k, out g, out hb0);
                ShipModel.Section(d, t1, out k, out g, out hb1);
                float z0 = t0 * d.length / 2f, z1 = t1 * d.length / 2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var rail = new GameObject("Rail").transform;
                    rail.SetParent(root, false);
                    Vector3 a0 = new Vector3(side * hb0 * 0.9f, 0f, z0), a1 = new Vector3(side * hb1 * 0.9f, 0f, z1);
                    rail.localPosition = (a0 + a1) / 2f + Vector3.up * (deckY + 0.4f);
                    rail.localRotation = Quaternion.LookRotation(a1 - a0);
                    var col = rail.gameObject.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.15f, 0.8f, (a1 - a0).magnitude + 0.1f);
                }
            }

            // The helmsman stands at the tiller, just forward of the sternpost, a step to starboard of the centre
            // line so the mizzen mast isn't right in front of his eyes.
            var helm = new GameObject("Helm").transform;
            helm.SetParent(root, false);
            helm.localPosition = HelmStation(d, deckY);
            parts.helm = helm;

            // Odin's altar on the deck before the main mast.
            float mast = d.sails.Length > 0 ? d.sails[0].x : 0f;
            var altar = new GameObject("Coin Altar").transform;
            altar.SetParent(root, false);
            altar.localPosition = new Vector3(0f, deckY + 0.05f, mast + 2.6f);
            LongshipBuilder.Deco(PrimitiveType.Cube, altar, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), Materials.Rock);
            LongshipBuilder.Deco(PrimitiveType.Cylinder, altar, new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.04f, 0.55f), Materials.Gold);
            parts.altar = altar;
            // A lantern on a post at the stern, lit when the sun goes down.
            float k2, g2, hb2;
            ShipModel.Section(d, -0.9f, out k2, out g2, out hb2);
            var post = new GameObject("Stern Lantern").transform;
            post.SetParent(root, false);
            post.localPosition = new Vector3(0f, g2 + 1.6f, -d.length / 2f * 0.9f);
            LongshipBuilder.Deco(PrimitiveType.Cube, post, Vector3.zero, new Vector3(0.35f, 0.5f, 0.35f), new Color(0.2f, 0.18f, 0.16f));
            LongshipBuilder.Deco(PrimitiveType.Cube, post, Vector3.zero, new Vector3(0.26f, 0.36f, 0.4f), new Color(1f, 0.82f, 0.45f));
            var lamp = post.gameObject.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.72f, 0.4f);
            lamp.range = 10f + d.length * 0.3f;
            lamp.intensity = 0f;
            lamp.shadows = LightShadows.None;
            parts.lantern = lamp;
            parts.floatPoints = new Vector3[0];
            return parts;
        }
    }
}
