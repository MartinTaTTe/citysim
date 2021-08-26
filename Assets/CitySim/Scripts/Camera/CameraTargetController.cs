using UnityEngine;
using Unity.CitySim.Map;

namespace Unity.CitySim.Camera
{
    public class CameraTargetController : MonoBehaviour
    {
        [Range(1f, 100f)]
        public float speed = 50f;

        [Range(10f, 100f)]
        public float maxRotationSpeed = 60f;

        [Range(10f, 100f)]
        public float rotationAcceleration = 50f;

        [Range(0f, 1000f)]
        public float spawnHeight = 100f;

        public Transform mainCameraTransform;
        public MapGenerator mapGenerator;

        float rotationSpeed = 0f;
        bool onGround = false;

        // Translate in the x, z plane
        public void TranslateFlat(float perpendicular, float parallel, float modifier)
        {
            // Move transform along the object's z-axis
            transform.Translate(perpendicular * speed * modifier, 0f, parallel * speed * modifier);
        }

        // Translate along the y axis
        public void TranslateVertical(float vertical, float modifier)
        {
            if (vertical > 0f)
                onGround = false;

            // Move transform along the object's z-axis
            transform.Translate(0f, vertical * speed * modifier, 0f);
        }

        // Rotate the target around its y-axis
        public void RotateAroundTarget(float frameAcceleration)
        {
            // Calculate this frame's rotation
            float rotation = 2f * frameAcceleration * rotationAcceleration;
            
            // Apply the rotation acceleration to the rotation speed
            rotationSpeed = Mathf.Clamp(rotationSpeed + rotation, -maxRotationSpeed, maxRotationSpeed);

            // Actual rotating is done in Update() (hence the {2f *} 5 lines above)
        }

        // Rotate the target around the camera's y-axis
        public void RotateAroundCamera(float movement)
        {
            // Calculate the rotation in degrees
            float rotation = movement / Screen.width * UnityEngine.Camera.main.fieldOfView * 2f;

            // Rotate around the global y-axis passing through the camera
            transform.RotateAround(mainCameraTransform.position, Vector3.up, rotation);
        }

        void Start()
        {
            // Get height of map
            float middle = mapGenerator.mapSize / 2;
            float mapHeight = mapGenerator.HeightAt(middle, middle) + spawnHeight;

            // Move camera to center of terrain
            transform.Translate(middle, mapHeight, middle);
        }

        void Update()
        {
            // Calculate this frame's rotation
            float rotation = Time.deltaTime * rotationAcceleration;

            // Reduce the rotation speed
            if (rotationSpeed < 0)
                rotationSpeed = Mathf.Min(rotationSpeed + rotation, 0);
            else if (rotationSpeed > 0)
                rotationSpeed = Mathf.Max(rotationSpeed - rotation, 0);

            // Rotate around the object's y-axis (should always be parallel to global y-axis)
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

            
            //// LAST STEPS ////
            // Clamp camera target to map borders
            Vector3 pos = transform.position;
            float max = mapGenerator.mapSize - 0.01f;
            pos.x = Mathf.Clamp(pos.x, 0, max);
            pos.z = Mathf.Clamp(pos.z, 0, max);

            // Prevent camera target from moving below the ground
            float height = mapGenerator.HeightAt(pos);
            if (onGround || pos.y < height) {
                onGround = true;
                pos.y = height;
            }

            transform.position = pos;
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}
