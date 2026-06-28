#if PRIME_TWEEN_INSTALLED
using PrimeTween;
using UnityEngine;

namespace PrimeTweenDemo
{
    public class Headlights : Animatable
    {
        [SerializeField] private AnimationCurve ease;
        [SerializeField] private Light[] lights;
        private bool isOn;

        public override void OnClick()
        {
            Animate(!isOn);
        }

        public override Sequence Animate(bool _isOn)
        {
            isOn = _isOn;
            var sequence = Sequence.Create();
            foreach (var _light in lights)
            {
                sequence.Group(Tween.LightIntensity(_light, _isOn ? 0.7f : 0, 0.8f, ease));
            }
            return sequence;
        }
    }
}
#endif