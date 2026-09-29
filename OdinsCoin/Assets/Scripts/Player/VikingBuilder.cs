using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A chunky low-poly Viking built from primitives: tunic, belt, trousers, boots, arms, a big beard,
    /// a nasal helmet, a round shield on the back and a bearded axe. Local +Z is forward, feet at y = 0.
    /// </summary>
    public static class VikingBuilder
    {
        public class Parts
        {
            public Transform root, body, leftLeg, rightLeg, leftArm, rightArm, axe, shield;
        }

        public static Parts Build(Transform root, Color tunic)
        {
            var p = new Parts { root = root };
            var skin = new Color(0.93f, 0.74f, 0.6f);
            var beard = new Color(0.72f, 0.42f, 0.18f);
            var leather = new Color(0.36f, 0.24f, 0.14f);
            var trousers = new Color(0.32f, 0.3f, 0.26f);
            var iron = new Color(0.55f, 0.56f, 0.58f);

            p.body = new GameObject("Body").transform;
            p.body.SetParent(root, false);

            // Legs pivot at the hip so they can swing.
            p.leftLeg = Limb(p.body, "Left Leg", new Vector3(-0.16f, 0.85f, 0f), new Vector3(0.2f, 0.8f, 0.22f), trousers, leather);
            p.rightLeg = Limb(p.body, "Right Leg", new Vector3(0.16f, 0.85f, 0f), new Vector3(0.2f, 0.8f, 0.22f), trousers, leather);

            // Torso: tunic, belt with a buckle.
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 1.2f, 0f), new Vector3(0.62f, 0.72f, 0.36f), tunic);
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 0.92f, 0f), new Vector3(0.64f, 0.1f, 0.38f), leather);
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 0.92f, 0.19f), new Vector3(0.12f, 0.08f, 0.04f), new Color(0.85f, 0.7f, 0.3f));
            // Fur collar.
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 1.55f, 0f), new Vector3(0.7f, 0.12f, 0.42f), new Color(0.5f, 0.42f, 0.34f));

            // Head, beard, helmet with nose guard.
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 1.8f, 0f), new Vector3(0.34f, 0.36f, 0.34f), skin);
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 1.66f, 0.13f), new Vector3(0.36f, 0.3f, 0.14f), beard);
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 1.5f, 0.17f), new Vector3(0.24f, 0.18f, 0.1f), beard);
            Part(PrimitiveType.Sphere, p.body, new Vector3(0f, 1.95f, 0f), new Vector3(0.4f, 0.3f, 0.4f), iron);
            Part(PrimitiveType.Cube, p.body, new Vector3(0f, 1.88f, 0.19f), new Vector3(0.05f, 0.18f, 0.04f), iron);
            Part(PrimitiveType.Cube, p.body, new Vector3(0.08f, 1.84f, 0.17f), new Vector3(0.05f, 0.04f, 0.02f), new Color(0.1f, 0.1f, 0.12f));
            Part(PrimitiveType.Cube, p.body, new Vector3(-0.08f, 1.84f, 0.17f), new Vector3(0.05f, 0.04f, 0.02f), new Color(0.1f, 0.1f, 0.12f));

            // Arms pivot at the shoulder.
            p.leftArm = Limb(p.body, "Left Arm", new Vector3(-0.39f, 1.5f, 0f), new Vector3(0.16f, 0.62f, 0.18f), tunic, skin);
            p.rightArm = Limb(p.body, "Right Arm", new Vector3(0.39f, 1.5f, 0f), new Vector3(0.16f, 0.62f, 0.18f), tunic, skin);

            // Bearded axe in the right hand.
            p.axe = new GameObject("Axe").transform;
            p.axe.SetParent(p.rightArm, false);
            p.axe.localPosition = new Vector3(0f, -0.62f, 0.05f);
            Part(PrimitiveType.Cylinder, p.axe, new Vector3(0f, 0f, 0.3f), new Vector3(0.05f, 0.4f, 0.05f), leather).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(PrimitiveType.Cube, p.axe, new Vector3(0f, -0.1f, 0.62f), new Vector3(0.04f, 0.3f, 0.18f), iron);

            // Round shield on the back: painted halves and an iron boss.
            p.shield = new GameObject("Shield").transform;
            p.shield.SetParent(p.body, false);
            p.shield.localPosition = new Vector3(0f, 1.25f, -0.24f);
            Part(PrimitiveType.Cylinder, p.shield, Vector3.zero, new Vector3(0.75f, 0.03f, 0.75f), new Color(0.75f, 0.15f, 0.12f)).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(PrimitiveType.Cube, p.shield, new Vector3(0f, 0f, -0.035f), new Vector3(0.74f, 0.12f, 0.01f), new Color(0.92f, 0.88f, 0.78f));
            Part(PrimitiveType.Sphere, p.shield, new Vector3(0f, 0f, -0.05f), new Vector3(0.16f, 0.16f, 0.1f), iron);
            return p;
        }

        /// <summary>A limb hanging from a pivot: the upper part in one colour, the end (hand/boot) in another.</summary>
        static Transform Limb(Transform parent, string name, Vector3 pivot, Vector3 size, Color main, Color end)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pivot;
            Part(PrimitiveType.Cube, t, new Vector3(0f, -size.y * 0.42f, 0f), new Vector3(size.x, size.y * 0.84f, size.z), main);
            Part(PrimitiveType.Cube, t, new Vector3(0f, -size.y * 0.92f, 0.03f), new Vector3(size.x * 1.1f, size.y * 0.18f, size.z * 1.3f), end);
            return t;
        }

        static Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            return LongshipBuilder.Deco(type, parent, pos, scale, color);
        }
    }
}
