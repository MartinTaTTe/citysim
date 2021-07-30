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

        [Range(1f, 10f)]
        public float zoomSpeed = 1f;

        float verticalOffset;
        float targetVerticalOffset;
        float horizontalOffset;
        float targetHorizontalOffset;
        GameObject cameraTarget;
        MapGenerator mapGenerator;

        void Awake()
        {
            cameraTarget = GameObject.FindGameObjectWithTag("Player");
            mapGenerator = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>();

            if (!cameraTarget)
                throw new System.Exception("Player not found!");

            verticalOffset = startVerticalOffset;
            targetVerticalOffset = verticalOffset;
            horizontalOffset = maxHorizontalOffset;
            targetHorizontalOffset = horizontalOffset;
        }

        void Update()
        {
            //// ZOOM ////
            // Get the zoom amount
            float zoom = -Input.mouseScrollDelta.y * zoomSensitivity;

            // If zooming in
            if (zoom < 0) {
                targetVerticalOffset += zoom;
                // If we are at the lowest allowed vertical offset
                if (targetVerticalOffset < 0) {
                    targetHorizontalOffset = System.Math.Max(targetHorizontalOffset + targetVerticalOffset, 0);
                    targetVerticalOffset = 0;
                }
            }

            // If zooming out
            if (zoom > 0) {
                targetHorizontalOffset += zoom;
                // If we are at highest allowed horizontal offset
                if (targetHorizontalOffset > maxHorizontalOffset) {
                    targetVerticalOffset += targetHorizontalOffset - maxHorizontalOffset;
                    targetHorizontalOffset = maxHorizontalOffset;
                }
            }

            // Smoothen camera movement during zooming
            verticalOffset += (targetVerticalOffset - verticalOffset) * zoomSpeed * Time.deltaTime;
            horizontalOffset += (targetHorizontalOffset - horizontalOffset) * zoomSpeed * Time.deltaTime;


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
            // Clamp camera to map borders
            Vector3 pos = transform.position;
            float max = mapGenerator.mapSize;
            pos.x = Mathf.Clamp(pos.x, 0, max);
            pos.z = Mathf.Clamp(pos.z, 0, max);

            // Prevent camera from moving within 1 unit from the ground
            float height = mapGenerator.HeightAt(pos);
            if (pos.y < height + 1)
                pos.y = height + 1;

            transform.position = pos;
        }
    }
}
