using UnityEngine;
using Unity.CitySim.Scenery;
using UnityEngine.UI;

namespace Unity.CitySim.UI
{
    public class ClockController : MonoBehaviour
    {
        public bool daylightSavingTime;
        public DaylightCycle daylightCycle;
        public Text text;

        void Update()
        {
            // Calculate clock time
            float time = daylightCycle.time;
            int hours = (int)(24f * time) + (daylightSavingTime ? 1 : 0);
            int minutes = (int)((1440f * time) % 60f);

            text.text = hours.ToString("D2") + ":" + minutes.ToString("D2");
        }
    }
}
