using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Turns a <see cref="VikingModel"/> into GameObjects: one transform per joint (so arms, legs and head can be
    /// animated) and one smooth mesh per colour on each joint. Local +Z is forward, feet at y = 0.
    /// </summary>
    public static class VikingBuilder
    {
        public class Parts
        {
            public Transform root, body, head, leftLeg, rightLeg, leftArm, rightArm, axe, shield;
            /// <summary>Only the storybook heroes have elbows; null on the old smooth Vikings.</summary>
            public Transform leftForearm, rightForearm;
            /// <summary>Where the weapon sits in the fist at rest, and the body's size (heroes only).</summary>
            public Vector3 axeRest;
            public float scale = 1f;
        }

        public static Parts Build(Transform root, Color tunic)
        {
            return Build(root, new VikingLook { tunic = tunic });
        }

        /// <summary>Also used for Saxons and raiders: a different tunic, beard, shield colour and a sword instead of an axe.</summary>
        public static Parts Build(Transform root, Color tunic, Color beard, Color shieldColor, bool sword)
        {
            return Build(root, new VikingLook { tunic = tunic, hair = beard, shield = shieldColor, sword = sword });
        }

        public static Parts Build(Transform root, VikingLook look)
        {
            var model = VikingModel.Build(look);
            if (root.GetComponent<OwnInk>() == null) root.gameObject.AddComponent<OwnInk>();
            var joints = new Dictionary<string, Transform>();
            foreach (var j in model.Joints)
            {
                var t = new GameObject(j.name).transform;
                t.SetParent(j.parent == null ? root : joints[j.parent], false);
                t.localPosition = j.localPosition;
                joints[j.name] = t;
            }
            foreach (var piece in model.Pieces)
            {
                var go = new GameObject(piece.joint + " Mesh");
                go.transform.SetParent(joints[piece.joint], false);
                go.AddComponent<MeshFilter>().sharedMesh = piece.mesh.ToMesh(piece.joint);
                go.AddComponent<MeshRenderer>().sharedMaterial = Materials.Get(piece.color);
            }
            return new Parts
            {
                root = root,
                body = joints[VikingModel.Body],
                head = joints[VikingModel.Head],
                leftLeg = joints[VikingModel.LeftLeg],
                rightLeg = joints[VikingModel.RightLeg],
                leftArm = joints[VikingModel.LeftArm],
                rightArm = joints[VikingModel.RightArm],
                axe = joints[VikingModel.Weapon],
                shield = joints[VikingModel.Shield],
            };
        }
    }
}
