using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapRenderController : MonoBehaviour
    {
        [Range(10, 2000)]
        public int minRenderRange = 1000;

        public Transform mainCameraTransform;
        public MapGenerator mapGenerator;

        void ToggleTerrainActivity()
        {
            Vector2 position = new Vector2(mainCameraTransform.position.x, mainCameraTransform.position.z);

            // Calculate render range
            float cameraHeight = mainCameraTransform.position.y + mapGenerator.HeightAt(mainCameraTransform.position);
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

        void Start()
        {
            ToggleTerrainActivity();
        }

        void Update()
        {
            ToggleTerrainActivity();
        }
    }
}

