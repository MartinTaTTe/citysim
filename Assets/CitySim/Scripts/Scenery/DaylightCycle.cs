using UnityEngine;

namespace Unity.CitySim.Scenery
{
    public class DaylightCycle : MonoBehaviour
    {
        [Range(0, 100)]
        public int speed = 10;

        void Update()
        {
            Vector3 rotation = new Vector3();

            // Brightness of the sky
            rotation.x = Time.deltaTime * speed;

            // Brightness of the light
            rotation.y = Time.deltaTime * speed;

            transform.Rotate(rotation);
        }
    }
}
