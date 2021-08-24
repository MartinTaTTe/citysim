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
        int currentShiftModifier;
        int currentCtrlModifier;

        public MapGenerator mapGenerator;
        public MapRenderController mapRenderController;
        public GUIController gUIController;
        public CameraController cameraController;
        
        // Reacts to a mouse click
        void OnMouseClick(Vector3 position, int shiftModifier, int ctrlModifier)
        {
            if (gUIController.toggleEditHeight.isOn)
                mapGenerator.ChangeHeight(position, Time.deltaTime * shiftModifier * ctrlModifier);
            else if (gUIController.toggleLevelHeight.isOn)
                mapGenerator.ChangeHeight(position, 0f, true);
        }
        
        void Update()
        {
            // Set correct values to the modifiers
            currentShiftModifier = Input.GetKey(KeyCode.LeftShift) ? shiftModifier : 1;
            currentCtrlModifier = Input.GetKey(KeyCode.LeftControl) ? ctrlModifier : 1;


            //// MOUSE EVENTS ////
            Vector3 mousePosition = mapRenderController.mousePosition;

            // If the pointer is on the terrain
            if (!gUIController.pointerOnGUI && mousePosition.y != -1f) {
                // If LMB is pressed
                if (Input.GetMouseButton(0)) {
                    OnMouseClick(mousePosition, currentShiftModifier, currentCtrlModifier);
                }
            }

            // If the scrollwheel is scrolled
            if (Input.mouseScrollDelta.y != 0f)
                cameraController.ZoomBy(Input.mouseScrollDelta.y);

            //// KEYBOARD EVENTS ////
            // If space is pressed
            if (Input.GetKey(KeyCode.Space))
                mapGenerator.DeleteAllChunks();
        }

        void OnValidate()
        {
            shiftModifier = shiftModifier < 50 ? 10 : 100;
            ctrlModifier = ctrlModifier < 1 ? -1 : 1;
        }
    }
}
