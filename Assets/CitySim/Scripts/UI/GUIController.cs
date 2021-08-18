using UnityEngine;
using UnityEngine.UI;
using Unity.CitySim.Map;

namespace Unity.CitySim.UI
{
    public class GUIController : MonoBehaviour
    {
        public bool pointerOnGUI { get; private set; }
        public InputController inputController;
        public GameObject editHeight;
        public Toggle toggleEditHeight;
        public GameObject levelHeight;
        public Toggle toggleLevelHeight;
        public Toggle toggleHeightEditorTools;
        public MapGenerator mapGenerator;
        public MapRenderController mapRenderController;
        public GameObject highlightTriangle;
        Mesh highlightTriangleMesh;

        public void ToggleHeightEditorTools()
        {
            toggleLevelHeight.isOn = false;
            toggleEditHeight.isOn = false;

            // Show/hide height editors and triangle highlighting
            bool toggle = toggleHeightEditorTools.isOn;
            editHeight.SetActive(toggle);
            levelHeight.SetActive(toggle);
            highlightTriangle.SetActive(toggle);
        }

        public void ToggleEditHeight()
        {
            bool toggle = toggleEditHeight.isOn;
            toggleLevelHeight.isOn = false;
            toggleEditHeight.isOn = toggle;
        }

        public void ToggleLevelHeight()
        {
            bool toggle = toggleLevelHeight.isOn;
            toggleEditHeight.isOn = false;
            toggleLevelHeight.isOn = toggle;
        }

        void Awake()
        {
            // Create the highlighted area mesh
            highlightTriangleMesh = new Mesh();
            highlightTriangleMesh.vertices = new Vector3[3];
            highlightTriangleMesh.triangles = new int[]{0, 1, 2};

            // Create the highlighted area game object
            highlightTriangle = Instantiate(highlightTriangle, transform.position, transform.rotation, transform);
            highlightTriangle.GetComponent<MeshFilter>().mesh = highlightTriangleMesh;
        }

        void Start()
        {
            ToggleHeightEditorTools();
        }

        void Update()
        {
            pointerOnGUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            
            // Update the highlighted area
            if (highlightTriangle.activeInHierarchy && mapRenderController.mousePosition.y != -1f) {
                Vector3[] vertices = mapGenerator.TriangleCorners(mapRenderController.mousePosition);
                if (vertices != null) {
                    highlightTriangleMesh.vertices = vertices;
                    highlightTriangleMesh.RecalculateBounds();
                }
            }
        }
    }
}
