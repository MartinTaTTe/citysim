using UnityEngine;
using Unity.CitySim.Map;

namespace Unity.CitySim.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Range(1f, 100f)]
        public float startHorizontalOffset = 10f;

        [Range(1, 10)]
        public int verticalExponent = 3;

        [Range(1f, 10f)]
        public float elevationDistance = 4f;

        [Range(0.1f, 10f)]
        public float zoomSensitivity = 1f;

        [Range(1f, 10f)]
        public float zoomSpeed = 1f;

        [Range(0f, 1f)]
        public float minZoomStep = 0.1f;

        float horizontalOffset;
        float targetHorizontalOffset;
        float verticalOffset;
        public Transform cameraTargetTransform;
        public MapGenerator mapGenerator;

        void Awake()
        {
            horizontalOffset = startHorizontalOffset;
            targetHorizontalOffset = horizontalOffset;
            verticalOffset = horizontalOffset * Mathf.Pow(horizontalOffset / elevationDistance, verticalExponent);
        }

        void Update()
        {
            //// ZOOM ////
            // Get the zoom amount
            float zoom = -Input.mouseScrollDelta.y * zoomSensitivity;

            // If zooming in
            if (zoom < 0)
                targetHorizontalOffset = Mathf.Max(targetHorizontalOffset + zoom, 0);
            // If zooming out
            else if (zoom > 0)
                targetHorizontalOffset += zoom;

            // Smoothen camera movement during zooming
            if (Mathf.Abs(targetHorizontalOffset - horizontalOffset) < minZoomStep)
                horizontalOffset = targetHorizontalOffset;
            else
                horizontalOffset += (targetHorizontalOffset - horizontalOffset) * zoomSpeed * Time.deltaTime;

            horizontalOffset = Mathf.Max(horizontalOffset, 0);
            verticalOffset = horizontalOffset * Mathf.Pow(horizontalOffset / elevationDistance, verticalExponent);


            //// OFFSET ////
            // Get the position of the target and correct the camera height
            Vector3 position = cameraTargetTransform.position;
            position.y += verticalOffset;

            // Set camera to that position and match rotation with target
            transform.SetPositionAndRotation(position, cameraTargetTransform.rotation);

            // Move camera backwards to be behind the target
            transform.Translate(Vector3.back * horizontalOffset);

            // Point camera towards target
            transform.LookAt(cameraTargetTransform);


            //// LAST STEPS ////
            // Clamp camera to map borders
            Vector3 pos = transform.position;
            float max = mapGenerator.mapSize - 0.01f;
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
