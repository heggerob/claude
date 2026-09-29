using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Builds a storybook hero in the scene from a <see cref="CharacterSpec"/>: one transform per joint, one mesh per
    /// colour per joint, the weapon and off-hand item attached to the hands, and a <see cref="ClothSway"/> that swings
    /// capes, banners and braids as the hero moves. Returns the same <see cref="VikingBuilder.Parts"/> the game's
    /// animation code already drives.
    /// </summary>
    public static class HeroBuilder
    {
        public static VikingBuilder.Parts Build(Transform root, CharacterSpec spec)
        {
            var model = HeroModel.Build(spec);
            var joints = new Dictionary<string, Transform>();
            foreach (var j in model.Joints)
            {
                var t = new GameObject(j.name).transform;
                t.SetParent(j.parent == null ? root : joints[j.parent], false);
                t.localPosition = j.localPosition;
                joints[j.name] = t;
            }
            AddPieces(model, joints);
            if (spec.weapon != WeaponId.None) AddPieces(HeroModel.BuildWeapon(spec), joints);
            if (spec.offHand != OffHandId.None) AddPieces(HeroModel.BuildOffHand(spec), joints);

            if (model.Swings.Count > 0)
            {
                var sway = root.gameObject.AddComponent<ClothSway>();
                foreach (var s in model.Swings) sway.Add(joints[s.joint], s.kind);
            }

            return new VikingBuilder.Parts
            {
                root = root,
                body = joints[Joints.Body],
                head = joints[Joints.Head],
                leftLeg = joints[Joints.LeftLeg],
                rightLeg = joints[Joints.RightLeg],
                leftArm = joints[Joints.LeftArm],
                rightArm = joints[Joints.RightArm],
                axe = joints[Joints.Weapon],
                shield = joints[Joints.OffHand],
            };
        }

        static void AddPieces(VikingModel model, Dictionary<string, Transform> joints)
        {
            foreach (var piece in model.Pieces)
            {
                Transform parent;
                if (!joints.TryGetValue(piece.joint, out parent)) continue;
                var go = new GameObject(piece.joint + (piece.ink ? " Ink" : " Mesh"));
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = piece.mesh.ToMesh(piece.joint);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Materials.Get(piece.color, piece.ink ? 0f : 0.1f);
                // The ink shells don't need to cast shadows of their own.
                if (piece.ink) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
