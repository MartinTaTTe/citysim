using UnityEngine;

namespace Unity.CitySim.UI
{
    public class CompassController : MonoBehaviour
    {
        public GameObject cameraTarget;

        void Update()
        {
            // Get the camera's rotation
            Quaternion rotation = cameraTarget.transform.rotation;

            // Move the rotatin to the correct axis
            rotation.z = rotation.y;
            rotation.y = 0f;

            transform.SetPositionAndRotation(transform.position, rotation);
        }
    }
}
