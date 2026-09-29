using UnityEngine;

namespace OdinsCoin
{
    public enum Gender { Male, Female }

    /// <summary>The body the player chooses. Everything else (hair, outfit, weapon, colours) is put on top.</summary>
    public class BodyShape
    {
        /// <summary>Metres to the top of the head (without headgear). 1.55 .. 1.95.</summary>
        public float height = 1.62f;
        /// <summary>Build: 0.8 = slim, 1 = normal, 1.25 = broad.</summary>
        public float width = 1f;
        public Gender gender = Gender.Male;

        public static float ClampHeight(float h) { return Mathf.Clamp(h, 1.4f, 2f); }
        public static float ClampWidth(float w) { return Mathf.Clamp(w, 0.75f, 1.35f); }
    }

    /// <summary>
    /// Measurements of a body, worked out from its <see cref="BodyShape"/>. Every garment is built from these,
    /// so any outfit fits any height, width and gender. Heights are from the soles up; the model faces +Z.
    /// Proportions come from the concept sheet: a head about a seventh of the height, a high belt, a long
    /// skirt and tall boots.
    /// </summary>
    public class Fit
    {
        public float height, width, s;         // s = height scale against the reference body
        public Gender gender;
        public float bootTop, knee, hip, waist, chest, shoulderY, neckY, headY, headR;
        public float shoulderX, hipX, waistR, chestR, hipR, depth;
        public float upperArm, foreArm, limbR;

        public const float ReferenceHeight = 1.62f;

        public static Fit Of(BodyShape body)
        {
            float h = BodyShape.ClampHeight(body.height);
            float w = BodyShape.ClampWidth(body.width);
            float s = h / ReferenceHeight;
            bool female = body.gender == Gender.Female;
            var f = new Fit { height = h, width = w, s = s, gender = body.gender };
            // The big head grows more slowly than the body, so short characters look younger and cuter.
            f.headR = 0.135f * Mathf.Pow(s, 0.35f);
            f.headY = h - f.headR;
            f.neckY = f.headY - f.headR - 0.01f * s;
            f.shoulderY = f.neckY - 0.08f * s;
            f.chest = f.shoulderY - 0.12f * s;
            f.waist = 0.62f * h;
            f.hip = f.waist - 0.08f * s;
            f.bootTop = 0.18f * h;
            f.knee = 0.29f * h;
            f.shoulderX = (female ? 0.15f : 0.17f) * w * s;
            f.hipX = 0.065f * w * s;
            f.chestR = (female ? 0.11f : 0.125f) * w * s;
            f.waistR = (female ? 0.095f : 0.11f) * w * s;
            f.hipR = (female ? 0.13f : 0.12f) * w * s;
            f.depth = 0.72f;
            f.upperArm = 0.24f * s;
            f.foreArm = 0.22f * s;
            f.limbR = 0.012f * Mathf.Sqrt(w);
            return f;
        }

        /// <summary>Radius of the torso (x; multiply by depth for z) at a height between the waist and shoulders.</summary>
        public float TorsoRadius(float y)
        {
            if (y <= waist) return waistR;
            if (y <= chest) return Mathf.Lerp(waistR, chestR, Mathf.SmoothStep(0f, 1f, (y - waist) / (chest - waist)));
            return Mathf.Lerp(chestR, chestR * 0.95f, (y - chest) / Mathf.Max(0.01f, shoulderY - chest));
        }
    }

    /// <summary>Joint names used by the character models (the old Viking's names, plus elbows and the off hand).</summary>
    public static class Joints
    {
        public const string Body = VikingModel.Body, Head = VikingModel.Head;
        public const string LeftLeg = VikingModel.LeftLeg, RightLeg = VikingModel.RightLeg;
        public const string LeftArm = VikingModel.LeftArm, RightArm = VikingModel.RightArm;
        public const string LeftForearm = "Left Forearm", RightForearm = "Right Forearm";
        public const string Weapon = VikingModel.Weapon, OffHand = "Off Hand", Back = VikingModel.Shield;
        // Swinging joints, made by the garments that need them.
        public const string Cape = "Cape", Tabard = "Tabard", LeftBraid = "Left Braid", RightBraid = "Right Braid";

        public static void Build(VikingModel m, Fit f)
        {
            m.AddJoint(Body, null, Vector3.zero);
            m.AddJoint(LeftLeg, Body, new Vector3(-f.hipX, f.hip, 0f));
            m.AddJoint(RightLeg, Body, new Vector3(f.hipX, f.hip, 0f));
            m.AddJoint(LeftArm, Body, new Vector3(-f.shoulderX, f.shoulderY, 0f));
            m.AddJoint(RightArm, Body, new Vector3(f.shoulderX, f.shoulderY, 0f));
            m.AddJoint(LeftForearm, LeftArm, new Vector3(0f, -f.upperArm, 0f));
            m.AddJoint(RightForearm, RightArm, new Vector3(0f, -f.upperArm, 0f));
            m.AddJoint(Head, Body, new Vector3(0f, f.neckY, 0f));
            m.AddJoint(Weapon, RightForearm, new Vector3(0f, -f.foreArm - 0.035f * f.s, 0.01f));
            m.AddJoint(OffHand, LeftForearm, new Vector3(0f, -f.foreArm - 0.035f * f.s, 0.01f));
            m.AddJoint(Back, Body, new Vector3(0f, f.waist + 0.15f * f.s, -f.chestR * f.depth - 0.05f));
        }
    }
}
