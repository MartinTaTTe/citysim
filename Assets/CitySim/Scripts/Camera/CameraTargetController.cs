using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CitySim.CameraTargetController
{
    public class CameraTargetController : MonoBehaviour
    {

        [Header("Movement")]
        [Range(1, 10)]
        public float speed = 5f;

        [Range(10, 100)]
        public float maxRotationSpeed = 60f;

        [Range(1, 10)]
        public float rotationAcceleration = 1f;

        [Range(1, 100)]
        public int shiftModifier = 10;

        float rotationSpeed = 0f;

        GameObject mainCamera;

        void Awake()
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            if (!mainCamera)
                throw new System.Exception("Camera not found!");
        }

        void Update()
        {
            // Set modifier to 'shiftModifier' if either shift is pressed
            int modifier = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? shiftModifier : 1;

            //// TRANSLATION ////
            // Get the horizontal and vertical axis.
            // By default they are mapped to the arrow keys.
            // The value is in the range -1 to 1
            float parallel = Input.GetAxis("Vertical") * speed - (Input.GetMouseButton(1) ? Input.GetAxis("Mouse Y") : 0);
            float perpendicular = Input.GetAxis("Horizontal") * speed;
            float vertical = 0f;

            // Set vertical if buttons 'R' or 'F' are pressed
            if (Input.GetKey(KeyCode.R))
                vertical += speed;
            if (Input.GetKey(KeyCode.F))
                vertical -= speed;

            // Make movement depend on time passed
            parallel *= Time.deltaTime;
            perpendicular *= Time.deltaTime;
            vertical *= Time.deltaTime;

            // Move translation along the object's z-axis
            transform.Translate(modifier * perpendicular, modifier * vertical, modifier * parallel);


            //// ROTATION AROUND TARGET////
            float rotation = 0f;
            
            // Set rotation if buttons 'Q' or 'E' are pressed
            if (Input.GetKey(KeyCode.Q))
                rotation -= rotationAcceleration;
            if (Input.GetKey(KeyCode.E))
                rotation += rotationAcceleration;

            // Apply the rotation acceleration to the rotation speed
            if (rotation == 0) {
                if (rotationSpeed < 0)
                    rotationSpeed = System.Math.Min(rotationSpeed + rotationAcceleration, 0);
                else if (rotationSpeed > 0)
                    rotationSpeed = System.Math.Max(rotationSpeed - rotationAcceleration, 0);
            } else // if there is rotation input
                rotationSpeed = System.Math.Max(
                    System.Math.Min(rotationSpeed + rotation, maxRotationSpeed),
                    -maxRotationSpeed
                );

            // Rotate around the object's y-axis (should always be parallel to global y-axis)
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);


            //// ROTATION AROUND CAMERA ////
            // Check if right mouse button is held down
            if (Input.GetMouseButton(1)) {

                // Get the movement of the mouse along the x-axis
                float movement = -Input.GetAxis("Mouse X");

                // Calculate the rotation in degrees
                rotation = movement / Screen.width * Camera.main.fieldOfView * 2;

                // Rotate around the global y-axis passing through the camera
                transform.RotateAround(mainCamera.transform.position, Vector3.up, rotation);
            }
        }
    }
}
