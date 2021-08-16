using UnityEngine;

namespace Unity.CitySim.Scenery
{
    public class DaylightCycle : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float time;
        public float cycleLength = 60f;
        public Vector3 noon;

        [Header("Sun")]
        public Light sun;
        public Gradient sunColor;
        public AnimationCurve sunIntensity;
        public AnimationCurve sunHeight;
        float height;

        [Header("Other lighting")]
        public AnimationCurve lightingIntenistyMultiplier;

        void Update()
        {
            // Update time
            time += 1f / cycleLength * Time.deltaTime;
            time %= 1f;

            // Update rotation
            Vector3 euler = (time) * noon * 4f;
            euler.x = sunHeight.Evaluate(time);
            sun.transform.eulerAngles = euler;

            // Update light intensity and color
            sun.intensity = sunIntensity.Evaluate(time);
            sun.color = sunColor.Evaluate(time);
            RenderSettings.ambientIntensity = lightingIntenistyMultiplier.Evaluate(time);
        }

    }
}
