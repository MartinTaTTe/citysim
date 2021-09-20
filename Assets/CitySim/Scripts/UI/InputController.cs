using UnityEngine;
using Unity.CitySim.Map;
using Unity.CitySim.Camera;

namespace Unity.CitySim.UI
{
    public class InputController : MonoBehaviour
    {
        [Range(10, 100)]
        public int shiftModifier = 10;

        [Range(-1, 1)]
        public int ctrlModifier = 10;

        [Range(10, 2000)]
        public int rayTraceDistance = 1000;

        [Range(0f, 1f)]
        public float rayTraceInterval = 1f;

        [Range(10f, 500f)]
        public float mouseSpeedDividend = 100f;

        public Vector3 mousePosition { get; private set; }
        public MapGenerator mapGenerator;
        public MapRenderController mapRenderController;
        public GUIController gUIController;
        public CameraController cameraController;
        public CameraTargetController cameraTargetController;
        
        int currentShiftModifier;
        int currentCtrlModifier;
        
        // Reacts to a mouse click
        void OnMouseClick(Vector3 position, int shiftModifier, int ctrlModifier)
        {
            if (gUIController.toggleEditHeight.isOn)
                mapGenerator.ChangeHeight(position, Time.deltaTime * shiftModifier * ctrlModifier);
            else if (gUIController.toggleLevelHeight.isOn)
                mapGenerator.ChangeHeight(position, 0f, true);
        }

        // Get the position of the mouse on the terrain
        Vector3 MousePosition()
        {
            // Ray from camera to mouse position
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);

            float d = 0f;
            while (d < rayTraceDistance) {
                Vector3 point = ray.GetPoint(d);

                // Check if point is below terrain surface
                if (point.y - mapGenerator.HeightAt(point) <= 0f) {
                    point.y = mapGenerator.HeightAt(point);
                    return point;
                }

                d += rayTraceInterval;
            }

            // No intersection between ray and terrain
            return new Vector3(-1f, -1f, -1f);
        }

        void Update()
        {
            // Set correct values to the modifiers
            currentShiftModifier = Input.GetKey(KeyCode.LeftShift) ? shiftModifier : 1;
            currentCtrlModifier = Input.GetKey(KeyCode.LeftControl) ? ctrlModifier : 1;

            //////////////////////
            //// MOUSE EVENTS ////
            //////////////////////
            mousePosition = MousePosition();

            // If the pointer is on the terrain
            if (!gUIController.pointerOnGUI && mousePosition.y != -1f) {
                // If LMB is pressed
                if (Input.GetMouseButton(0)) {
                    OnMouseClick(mousePosition, currentShiftModifier, currentCtrlModifier);
                }
            }

            // If RMB is pressed
            if (Input.GetMouseButton(1)) {
                cameraTargetController.TranslateFlat(0f, -Input.GetAxis("Mouse Y"), currentShiftModifier / mouseSpeedDividend);
                cameraTargetController.RotateAroundCamera(-Input.GetAxis("Mouse X"));
            }

            // If the scrollwheel is scrolled
            if (Input.mouseScrollDelta.y != 0f)
                cameraController.ZoomBy(Input.mouseScrollDelta.y);

            /////////////////////////
            //// KEYBOARD EVENTS ////
            /////////////////////////
            // If space is pressed
            if (Input.GetKey(KeyCode.Space))
                mapGenerator.DeleteAllChunks();

            // If Q is pressed
            if (Input.GetKey(KeyCode.Q))
                cameraTargetController.RotateAroundTarget(-Time.deltaTime);

            // If E is pressed
            if (Input.GetKey(KeyCode.E))
                cameraTargetController.RotateAroundTarget(Time.deltaTime);

            // If R is pressed
            if (Input.GetKey(KeyCode.R))
                cameraTargetController.TranslateVertical(1f, currentShiftModifier * Time.deltaTime);

            // If F is pressed
            if (Input.GetKey(KeyCode.F))
                cameraTargetController.TranslateVertical(-1f, currentShiftModifier * Time.deltaTime);

            // Move camera target with WASD or arrow keys
            cameraTargetController.TranslateFlat(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), currentShiftModifier * Time.deltaTime);
        }

        void OnValidate()
        {
            shiftModifier = shiftModifier < 50 ? 10 : 100;
            ctrlModifier = ctrlModifier < 1 ? -1 : 1;
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawSphere(mousePosition, 0.2f);
        }
    }
}
