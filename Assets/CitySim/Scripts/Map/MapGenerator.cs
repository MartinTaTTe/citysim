using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Range(1, 500)]
        public int maxHeight = 100;

        [Range(16, 4096)]
        public int chunkSize = 512;

        [Range(0.1f, 2f)]
        public float levelOfDetail = 1f;

        [Range(1, 10)]
        public float intensity = 5f;

        [Range(2, 100)]
        public int maxGridSize = 10;
        public Vector2 perlinOffset = new Vector2(0f, 0f);

        public GameObject TerrainType;

        GameObject[,] terrainChunks;


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

        void Awake()
        {
            terrainChunks = new GameObject[maxGridSize, maxGridSize];
        }

        void OnValidate()
        {
            // Max Grid Size
            maxGridSize = maxGridSize / 2 * 2;
        }
    } 
}
