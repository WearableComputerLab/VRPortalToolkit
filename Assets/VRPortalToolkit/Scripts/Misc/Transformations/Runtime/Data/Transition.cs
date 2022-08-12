using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Transformations
{
    [System.Serializable]
    public class Transition
    {
        [SerializeField] private TimeUnit _timeUnit = TimeUnit.TimeScaled;
        public TimeUnit timeUnit { get => _timeUnit; set => _timeUnit = value; }

        [SerializeField] private TransitionMode _mode = TransitionMode.Instant;
        public TransitionMode mode { get => _mode; set => _mode = value; }

        [SerializeField] private float _stepAmount = 0.5f;
        public float stepAmount { get => _stepAmount; set => _stepAmount = value; }

        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve curve { get => _curve; set => _curve = value; }

        public Transition(TransitionMode mode = TransitionMode.Instant, TimeUnit timeUnit = TimeUnit.TimeScaled, float stepAmmount = 0.5f)
        {
            this.mode = mode;
            this.timeUnit = timeUnit;
        }

        public Transition(AnimationCurve curve, TimeUnit timeUnit = TimeUnit.TimeScaled, float stepAmount = 0.5f)
        {
            mode = TransitionMode.Curve;
            this.curve = curve;
            this.timeUnit = timeUnit;
            this.stepAmount = stepAmount;
        }

        public float StepAngle(float from, float to, float timeStep)
        {
            StepAngle(from, ref to, timeStep);
            return to;
        }

        public void StepAngle(float from, ref float to, float timeStep)
        {
            switch (mode)
            {
                case TransitionMode.Lerp:
                    to = Mathf.LerpAngle(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.SmoothStep:
                    // TODO: Not sure what this should be
                    to = Mathf.SmoothStep(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.MoveTowards:
                    to = Mathf.MoveTowardsAngle(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.Curve:
                    if (curve != null) to = Mathf.LerpAngle(from, to, curve.Evaluate(stepAmount * timeStep));
                    break;
            }
        }

        public float StepFloat(float from, float to, float timeStep)
        {
            StepFloat(from, ref to, timeStep);
            return to;
        }

        public void StepFloat(float from, ref float to, float timeStep)
        {
            switch (mode)
            {
                case TransitionMode.Lerp:
                    to = Mathf.Lerp(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.SmoothStep:
                    to = Mathf.SmoothStep(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.MoveTowards:
                    to = Mathf.MoveTowards(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.Curve:
                    if (curve != null) to = Mathf.Lerp(from, to, curve.Evaluate(stepAmount * timeStep));
                    break;
            }
        }

        public Vector3 StepPosition(Vector3 from, Vector3 to, float timeStep)
        {
            StepPosition(from, ref to, timeStep);
            return to;
        }

        public void StepPosition(Vector3 from, ref Vector3 to, float timeStep)
        {
            switch (mode)
            {
                case TransitionMode.Lerp:
                    to = Vector3.Lerp(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.SmoothStep:
                    to = TransformUtilities.SmoothStepVector3(from, to, stepAmount);
                    break;
                case TransitionMode.MoveTowards:
                    to = Vector3.MoveTowards(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.Curve:
                    if (curve != null) to = Vector3.Lerp(from, to, curve.Evaluate(stepAmount * timeStep));
                    break;
            }
        }

        public Quaternion StepRotation(Quaternion from, Quaternion to, float timeStep)
        {
            StepRotation(from, ref to, timeStep);
            return to;
        }

        public void StepRotation(Quaternion from, ref Quaternion to, float timeStep)
        {
            switch (mode)
            {
                case TransitionMode.Lerp:
                    to = Quaternion.Lerp(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.SmoothStep:
                    to = TransformUtilities.SmoothStepQuaternion(from, to, stepAmount);
                    break;
                case TransitionMode.MoveTowards:
                    to = Quaternion.RotateTowards(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.Curve:
                    if (curve != null) to = Quaternion.Lerp(from, to, curve.Evaluate(stepAmount * timeStep));
                    break;
            }
        }

        public Vector3 StepScale(Vector3 from, Vector3 to, float timeStep)
        {
            StepScale(from, ref to, timeStep);
            return to;
        }

        public void StepScale(Vector3 from, ref Vector3 to, float timeStep)
        {
            switch (mode)
            {
                case TransitionMode.Lerp:
                    to = Vector3.Lerp(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.SmoothStep:
                    to = TransformUtilities.SmoothStepVector3(from, to, stepAmount);
                    break;
                case TransitionMode.MoveTowards:
                    to = Vector3.MoveTowards(from, to, stepAmount * timeStep);
                    break;
                case TransitionMode.Curve:
                    if (curve != null) to = Vector3.Lerp(from, to, curve.Evaluate(stepAmount * timeStep));
                    break;
            }
        }
    }
}