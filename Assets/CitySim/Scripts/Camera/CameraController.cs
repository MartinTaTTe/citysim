using UnityEngine;
using Unity.CitySim.Map;

namespace Unity.CitySim.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Range(1, 100)]
        public float startVerticalOffset = 10f;

        [Range(1, 100)]
        public float maxHorizontalOffset = 10f;

        [Range(0.1f, 10f)]
        public float zoomSensitivity = 1f;

        float verticalOffset;
        float horizontalOffset;
        GameObject cameraTarget;
        MapGenerator mapGenerator;

        void Awake()
        {
            cameraTarget = GameObject.FindGameObjectWithTag("Player");
            mapGenerator = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>();

            if (!cameraTarget)
                throw new System.Exception("Player not found!");

            verticalOffset = startVerticalOffset;
            horizontalOffset = maxHorizontalOffset;
        }

        void Update()
        {
            //// ZOOM ////
            // Get the zoom amount
            float zoom = -Input.mouseScrollDelta.y * zoomSensitivity;

            // If zooming in
            if (zoom < 0) {
                verticalOffset += zoom;
                // If we are at the lowest allowed vertical offset
                if (verticalOffset < 0) {
                    horizontalOffset = System.Math.Max(horizontalOffset + verticalOffset, 0);
                    verticalOffset = 0;
                }
            }

            // If zooming out
            if (zoom > 0) {
                horizontalOffset += zoom;
                // If we are at highest allowed horizontal offset
                if (horizontalOffset > maxHorizontalOffset) {
                    verticalOffset += horizontalOffset - maxHorizontalOffset;
                    horizontalOffset = maxHorizontalOffset;
                }
            }


            //// OFFSET ////
            // Get the position of the target and correct the camera height
            Vector3 position = cameraTarget.transform.position;
            position.y += verticalOffset;

            // Set camera to that position and match rotation with target
            transform.SetPositionAndRotation(position, cameraTarget.transform.rotation);

            // Move camera backwards to be behind the target
            transform.Translate(Vector3.back * horizontalOffset);

            // Point camera towards target
            transform.LookAt(cameraTarget.transform);


            //// LAST STEPS ////
            // Prevent camera target from moving below the ground
            Vector3 pos = transform.position;
            float height = mapGenerator.HeightAt(transform.position);
            if (pos.y < height + 1)
                pos.y = height + 1;
            transform.position = pos;
        }
    }
}
