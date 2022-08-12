using UnityEngine;

namespace Misc.Transformations
{
    public static class TransformUtilities
    {
        public static Transform FindRecursive(this Transform parent, string name)
        {
            if (parent.name == name) return parent;

            Transform found;

            foreach (Transform child in parent)
            {
                found = child.FindRecursive(name);

                if (found) return found;
            }

            return null;
        }

        public static void StepPosition(Vector3 from, ref Vector3 to, TransitionMode transition = TransitionMode.Instant, float step = 1f)
        {
            if (transition != TransitionMode.Instant)
            {
                if (transition == TransitionMode.Lerp)
                    to = Vector3.Lerp(from, to, step);
                else if (transition == TransitionMode.SmoothStep)
                    to = SmoothStepVector3(from, to, step);
                else
                    to = Vector3.MoveTowards(from, to, step);
            }
        }

        public static void StepRotation(Quaternion from, ref Quaternion to, TransitionMode transition = TransitionMode.Instant, float step = 1f)
        {
            if (transition != TransitionMode.Instant)
            {
                if (transition == TransitionMode.Lerp)
                    to = Quaternion.Lerp(from, to, step);
                else if (transition == TransitionMode.SmoothStep)
                    to = SmoothStepQuaternion(from, to, step);
                else
                    to = Quaternion.RotateTowards(from, to, step);
            }
        }

        public static void StepScale(Vector3 from, ref Vector3 to, TransitionMode transition = TransitionMode.Instant, float step = 1f)
        {
            if (transition != TransitionMode.Instant)
            {
                if (transition == TransitionMode.Lerp)
                    to = Vector3.Lerp(from, to, step);
                else if (transition == TransitionMode.SmoothStep)
                    to = SmoothStepVector3(from, to, step);
                else
                    to = Vector3.MoveTowards(from, to, step);
            }
        }

        public static Vector3 SmoothStepVector3(Vector3 from, Vector3 to, float time)
        {
            Vector3 dirVec = (to - from);
            return from + dirVec.normalized * Mathf.SmoothStep(0, dirVec.magnitude, time);
        }

        public static Quaternion SmoothStepQuaternion(Quaternion from, Quaternion to, float time)
        {
            float degrees = Mathf.SmoothStep(0, Quaternion.Angle(from, to), time);
            return Quaternion.RotateTowards(from, to, degrees);
        }
    }
}