using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using Unity.Mathematics;

namespace Syadeu.Presentation.Dotween
{
    internal sealed class Float3Plugin : ABSTweenPlugin<float3, float3, Float3Options>
    {
        // Leave this empty
        public override void Reset(TweenerCore<float3, float3, Float3Options> t) { }

        // Sets the values in case of a From tween
        public override void SetFrom(TweenerCore<float3, float3, Float3Options> t, bool isRelative)
        {
            float3 prevEndVal = t.endValue;
            t.endValue = t.getter();
            t.startValue = isRelative ? t.endValue + prevEndVal : prevEndVal;
            t.setter.Invoke(t.startValue);
        }
        // Sets the values in case of a From tween with a specific from value
        public override void SetFrom(TweenerCore<float3, float3, Float3Options> t, float3 fromValue, bool setImmediately, bool isRelative)
        {
            t.startValue = fromValue;
            if (setImmediately) t.setter.Invoke(fromValue);
        }
        // Used by special plugins, just let it return the given value
        public override float3 ConvertToStartValue(TweenerCore<float3, float3, Float3Options> t, float3 value)
        {
            return value;
        }
        // Determines the correct endValue in case this is a relative tween
        public override void SetRelativeEndValue(TweenerCore<float3, float3, Float3Options> t)
        {
            t.endValue = t.startValue + t.changeValue;
        }
        // Sets the overall change value of the tween
        public override void SetChangeValue(TweenerCore<float3, float3, Float3Options> t)
        {
            t.changeValue = t.endValue - t.startValue;
        }
        // Converts a regular duration to a speed-based duration
        public override float GetSpeedBasedDuration(Float3Options options, float unitsXSecond, float3 changeValue)
        {
            // Not implemented in this case (but you could implement your own logic to convert duration to units x second)
            return unitsXSecond;
        }
        // Calculates the value based on the given time and ease
        public override void EvaluateAndApply(Float3Options options, Tween t, bool isRelative, DOGetter<float3> getter, DOSetter<float3> setter, float elapsed, float3 startValue, float3 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
        {
            float easeVal = EaseManager.Evaluate(t, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);

            startValue += changeValue * easeVal;
            setter.Invoke(startValue);
        }
    }
}
