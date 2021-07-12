using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map parameters")]

        [Range(10, 1000)]
        public int chunkSize = 512;

        [Range(0.5f, 10f)]
        public float levelOfDetail = 1f;

        [Range(2, 100)]
        public int maxGridSize = 10;

        [Header("Perlin noise")]
        public Vector2 perlinOffset = new Vector2(0f, 0f);

        [Range(1, 10)]
        public int octaves = 4;

        [Range(1f, 10000f)]
        public float initialAmplitude = 1f;

        [Range(0.1f, 2f)]
        public float persistance = 0.5f;

        [Range(0.01f, 3f)]
        public float initialFrequency = 1f;

        [Range(0.1f, 10f)]
        public float lacunarity = 2f;

        [Header("")]
        public GameObject TerrainType;

        GameObject[,] terrainChunks;
        public float mapSize;

        // Returns the terrain chunk at x, y and generates it if necessary
        public GameObject GetChunk(int x, int y)
        {
            // Make sure x, y are within limits
            if (x < 0 || y < 0 || x >= maxGridSize || y >= maxGridSize)
                return null;

            // If it exists
            if (terrainChunks[x, y])
                return terrainChunks[x, y];

            // If it doesn't exist
            return GenerateChunk(x, y);
        }

        // Returns the terrain chunk at x, y only if it exists
        public GameObject GetChunkIfExist(int x, int y)
        {
            // Make sure x, y are within limits
            if (x < 0 || y < 0 || x >= maxGridSize || y >= maxGridSize)
                return null;

            // If it exists
            if (terrainChunks[x, y])
                return terrainChunks[x, y];

            // If it doesn't exist
            return null;
        }

        // Get the grid coordinates from a world position
        public Vector2Int GetGridCoords(float x, float y)
        {
            int xInt, yInt;

            // Translate the world space coordinates to grid coordinates
            xInt = (int)(x / (chunkSize * levelOfDetail));
            yInt = (int)(y / (chunkSize * levelOfDetail));

            // Clamp coordinates to be within limits
            xInt = Mathf.Clamp(xInt, 0, maxGridSize - 1);
            yInt = Mathf.Clamp(yInt, 0, maxGridSize - 1);

            return new Vector2Int(xInt, yInt);
        }
        public Vector2Int GetGridCoords(Vector3 position)
        {
            return GetGridCoords(position.x, position.z);
        }

        // Get the height of the terrain from a world position
        public float HeightAt(Vector3 position)
        {
            // Get the position in grid coordinates
            Vector2Int gridPos = GetGridCoords(position);

            // Get the chunk where we are
            GameObject terrain = GetChunkIfExist(gridPos.x, gridPos.y);

            // If height requested in undefined chunk
            if (terrain == null)
                return 0f;

            // Get the offset within the chunk
            Vector2 localOffset = new Vector2(
                position.x - (gridPos.x * chunkSize * levelOfDetail),
                position.z - (gridPos.y * chunkSize * levelOfDetail)
            );

            return terrain.GetComponent<TerrainGenerator>().HeightAt(localOffset);
        }

        // Create a new terrain chunk at grid coordinates x, y
        GameObject GenerateChunk(int x, int y)
        {
            // Copy the Terrain Type
            terrainChunks[x,y] = Instantiate(TerrainType, this.transform.position, this.transform.rotation, this.transform);

            // Set the offset for the new terrain chunk
            Vector2 offset = new Vector2(x * chunkSize * levelOfDetail, y * chunkSize * levelOfDetail);
            terrainChunks[x,y].GetComponent<TerrainGenerator>().SetOffset(offset);
            
            return terrainChunks[x, y];
        }

        // Set the size of the map in units
        void SetMapSize()
        {
            mapSize = chunkSize * levelOfDetail * maxGridSize;
        }

        void Awake()
        {
            SetMapSize();
            terrainChunks = new GameObject[maxGridSize, maxGridSize];
        }

        void OnValidate()
        {
            // Max Grid Size
            maxGridSize = maxGridSize / 2 * 2;

            SetMapSize();
        }

        void Update()
        {
            // Delete all chunks if space is pressed
            if (Input.GetKey(KeyCode.Space)) {
                for (int x = 0; x < maxGridSize; x++) {
                    for (int y = 0; y < maxGridSize; y++) {
                        GameObject chunk = GetChunkIfExist(x, y);
                        if (chunk)
                            Destroy(chunk);
                    }
                }
            }

        }
    } 
}
