using UnityEngine;

namespace Unity.CitySim.UI
{
    public class CompassController : MonoBehaviour
    {
        public GameObject cameraTarget;

        void Update()
        {
            // Get the camera's rotation
            Vector3 rotation = cameraTarget.transform.eulerAngles;

            // Move the rotatin to the correct axis
            rotation.z = rotation.y;
            rotation.y = 0f;

            transform.eulerAngles = rotation;
        }
    }
}
