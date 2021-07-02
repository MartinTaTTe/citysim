using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapRenderController : MonoBehaviour
    {
        [Range(10, 10000)]
        public int renderRange = 5000;
        
        GameObject cameraTarget;
        MapGenerator mapGenerator;

        void ToggleTerrainActivity()
        {
            Vector2 position = new Vector2(cameraTarget.transform.position.x, cameraTarget.transform.position.z);

            // Get the start coordinates
            Vector2Int from = mapGenerator.GetGridCoords(position.x - renderRange, position.y - renderRange);

            // Get the end coordinates
            Vector2Int to = mapGenerator.GetGridCoords(position.x + renderRange, position.y + renderRange);

            // Deactivate all terrains
            for (int x = 0; x < mapGenerator.maxGridSize; x++) {
                for (int y = 0; y < mapGenerator.maxGridSize; y++) {
                    GameObject chunk = mapGenerator.GetChunkIfExist(x, y);
                    if (chunk)
                        chunk.SetActive(false);
                }
            }

            // Activate all terrains within render distance
            for (int x = from.x; x <= to.x; x++) {
                for (int y = from.y; y <= to.y; y++) {
                    mapGenerator.GetChunk(x, y).SetActive(true);
                }
            }
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
        }
    }
}

