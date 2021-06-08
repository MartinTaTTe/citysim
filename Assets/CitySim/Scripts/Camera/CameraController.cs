using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CitySim.CameraController
{
    public class CameraController : MonoBehaviour
    {

        [Header("Offset")]
        [Range(1, 100)]
        public float startVerticalOffset = 10f;

        [Range(1, 100)]
        public float maxHorizontalOffset = 10f;

        [Range(0.1f, 10f)]
        public float zoomSensitivity = 1f;

        GameObject target;
        float verticalOffset;
        float horizontalOffset;

        void Awake()
        {
            target = GameObject.FindGameObjectWithTag("Player");

            if (!target)
                throw new System.Exception("Player not found!");

            verticalOffset = startVerticalOffset;
            horizontalOffset = maxHorizontalOffset;
        }

        void Update()
        {
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

            // Get the position of the target and correct the camera height
            Vector3 position = target.transform.position;
            position.y += verticalOffset;

            // Set camera to that position and match rotation with target
            transform.SetPositionAndRotation(position, target.transform.rotation);

            // Move camera backwards to be behind the target
            transform.Translate(Vector3.back * horizontalOffset);

            // Point camera towards target
            transform.LookAt(target.transform);
        }
    }
}
