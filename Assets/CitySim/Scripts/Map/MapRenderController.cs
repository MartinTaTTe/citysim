using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapRenderController : MonoBehaviour
    {
        [Range(10, 2000)]
        public int minRenderRange = 1000;

        [Range(10, 2000)]
        public int rayTraceDistance = 1000;

        [Range(0f, 1f)]
        public float rayTraceInterval = 1f;

        public GameObject highlight;
        
        GameObject mainCamera;
        MapGenerator mapGenerator;
        public Vector3 mousePosition { get; private set; }
        Mesh mesh;

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

        void ToggleTerrainActivity()
        {
            Vector2 position = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.z);

            // Calculate render range
            float cameraHeight = mainCamera.transform.position.y + mapGenerator.HeightAt(mainCamera.transform.position);
            float renderRange = Mathf.Max(minRenderRange, cameraHeight);

            // Get the start coordinates
            Vector2Int from = mapGenerator.ToGridCoords(position.x - renderRange, position.y - renderRange);

            // Get the end coordinates
            Vector2Int to = mapGenerator.ToGridCoords(position.x + renderRange, position.y + renderRange);

            for (int x = 0; x < mapGenerator.maxGridSize; x++) {
                for (int y = 0; y < mapGenerator.maxGridSize; y++) {
                    // Activate terrains within render
                    if (InRange(x, from.x, to.x) && InRange(y, from.y, to.y)) {
                        mapGenerator.GetChunk(x, y).SetActive(true);
                    }
                    // Deactivate any terrains outside render
                    else if (mapGenerator.GetChunkIfExist(x, y)) {
                        mapGenerator.GetChunkIfExist(x, y).SetActive(false);
                    }
                }
            }
        }

        bool InRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        void Awake()
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            mapGenerator = GetComponent<MapGenerator>();

            // Create the highlighted area mesh
            mesh = new Mesh();
            mesh.vertices = new Vector3[3];
            mesh.triangles = new int[]{0, 1, 2};

            // Create the highlighted area game object
            highlight = Instantiate(highlight, this.transform.position, this.transform.rotation, this.transform);
            highlight.GetComponent<MeshFilter>().mesh = mesh;
        }

        void Start()
        {
            ToggleTerrainActivity();
        }

        void Update()
        {
            ToggleTerrainActivity();

            mousePosition = MousePosition();

            // Update the highlighted area
            if (mousePosition.y != -1f) {
                Vector3[] vertices = mapGenerator.TriangleCorners(mousePosition);
                if (vertices != null) {
                    mesh.vertices = vertices;
                    mesh.RecalculateBounds();
                }
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawSphere(mousePosition, 0.2f);
        }
    }
}

