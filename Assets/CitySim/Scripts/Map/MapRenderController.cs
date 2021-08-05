using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapRenderController : MonoBehaviour
    {
        [Range(10, 10000)]
        public int renderRange = 5000;

        [Range(10, 2000)]
        public int rayTraceDistance = 1000;

        [Range(0f, 1f)]
        public float rayTraceInterval = 1f;
        
        GameObject cameraTarget;
        MapGenerator mapGenerator;
        Vector3 mousePosition;

        // Get the position of the mouse on the terrain
        public Vector3 MousePosition()
        {
            // Ray from camera to mouse position
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);

            float d = 0f;
            while (d < rayTraceDistance) {
                Vector3 point = ray.GetPoint(d);

                // Check if point passes through or near terrain surface, works flawlessly up to 60 degrees sloping
                if (Mathf.Abs(point.y - mapGenerator.HeightAt(point)) < rayTraceInterval) {
                    point.y = mapGenerator.HeightAt(point);
                    return point;
                }

                d += rayTraceInterval;
            }

            // No intersection between ray and terrain
            return new Vector3();
        }

        void ToggleTerrainActivity()
        {
            Vector2 position = new Vector2(cameraTarget.transform.position.x, cameraTarget.transform.position.z);

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
            cameraTarget = GameObject.FindGameObjectWithTag("Player");
            mapGenerator = GetComponent<MapGenerator>();
        }

        void Start()
        {
            ToggleTerrainActivity();
        }

        void Update()
        {
            ToggleTerrainActivity();

            mousePosition = MousePosition();
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawSphere(mousePosition, 0.2f);
        }
    }
}

