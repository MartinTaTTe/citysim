using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map parameters")]

        [Range(0, 255)]
        public int quadsPerChunk = 250;

        [Range(0.5f, 10f)]
        public float quadSize = 1f;

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
        [Range(0f, 1f)]
        public float waterLevel = 0.4f;
        public Color water;

        [Range(0f, 0.2f)]
        public float landStep = 0.1f;
        public Color[] land;
        public Gradient gradient { get; private set; }
        public GameObject TerrainType;
        public float mapSize;
        public float chunkSize;
        public int[] triangles { get; private set; }
        public Vector2 currentChunkOffset { get; private set; }

        GameObject[,] terrainChunks;
        GradientColorKey[] colorKey;
        GradientAlphaKey[] alphaKey;
        MapRenderController mapRenderController;

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
        public Vector2Int ToGridCoords(float x, float y)
        {
            int xInt, yInt;

            // Translate the world space coordinates to grid coordinates
            xInt = (int)(x / (chunkSize));
            yInt = (int)(y / (chunkSize));

            // Clamp coordinates to be within limits
            xInt = Mathf.Clamp(xInt, 0, maxGridSize - 1);
            yInt = Mathf.Clamp(yInt, 0, maxGridSize - 1);

            return new Vector2Int(xInt, yInt);
        }
        public Vector2Int ToGridCoords(Vector3 position)
        {
            return ToGridCoords(position.x, position.z);
        }

        // Get the height of the terrain from a world position, prioritize performance!
        public float HeightAt(float x, float y) {
            // Ensure position is within map borders
            if (x < 0f || y < 0f || x >= mapSize || y >= mapSize)
                return 0f;

            // Get the position in grid coordinates
            Vector2Int gridPos = ToGridCoords(x, y);

            // Get the chunk where we are
            GameObject terrain = GetChunk(gridPos.x, gridPos.y);

            // If height requested in undefined chunk
            if (terrain == null)
                return 0f;

            // Get the offset within the chunk
            float xOff = x - (gridPos.x * chunkSize);
            float yOff = y - (gridPos.y * chunkSize);

            return terrain.GetComponent<TerrainGenerator>().HeightAt(xOff, yOff);
        }

        public float HeightAt(Vector3 position)
        {
            return HeightAt(position.x, position.z);
        }

        // Change the height of all surrounding verticies by 'change'
        void ChangeHeight(float x, float y, float change)
        {
            // Ensure position is within map borders
            if (x < 0f || y < 0f || x >= mapSize || y >= mapSize)
                return;

            // Get the position in grid coordinates
            Vector2Int gridPos = ToGridCoords(x, y);

            // Get the chunk where we are
            GameObject terrain = GetChunk(gridPos.x, gridPos.y);

            // If height requested in undefined chunk
            if (!terrain)
                return;

            // Get the offset within the chunk
            float xOff = x - (gridPos.x * chunkSize);
            float yOff = y - (gridPos.y * chunkSize);
            
            terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff, yOff, change);
            
            // Get the offsets of affected neighbors
            Vector2Int neighborOffset = new Vector2Int(0, 0);

            if (xOff < quadSize)
                neighborOffset.x = -1;
            else if (xOff >= chunkSize - quadSize)
                neighborOffset.x = 1;

            if (yOff < quadSize)
                neighborOffset.y = -1;
            else if (yOff >= chunkSize - quadSize)
                neighborOffset.y = 1;

            // Change height of affected neighbors
            if (neighborOffset.x != 0) {
                terrain = GetChunk(gridPos.x + neighborOffset.x, gridPos.y);
                if (terrain)
                    terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff - chunkSize, yOff, change);
            }
            if (neighborOffset.y != 0) {
                terrain = GetChunk(gridPos.x, gridPos.y + neighborOffset.y);
                if (terrain)
                    terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff, yOff - chunkSize, change);
            }
            if (neighborOffset.x != 0 && neighborOffset.y != 0) {
                terrain = GetChunk(gridPos.x + neighborOffset.x, gridPos.y + neighborOffset.y);
                if (terrain)
                    terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff - chunkSize, yOff - chunkSize, change);
            }
        }

        void ChangeHeight(Vector3 position, float change)
        {
            if (mapRenderController.mousePosition.y != -1)
                ChangeHeight(position.x, position.z, change);
        }

        // Delete all chunks in order to regenerate them
        void DeleteAllChunks()
        {
            for (int x = 0; x < maxGridSize; x++) {
                for (int y = 0; y < maxGridSize; y++) {
                    GameObject chunk = GetChunkIfExist(x, y);
                    if (chunk)
                        Destroy(chunk);
                }
            }
        }

        // Create the triangle array used by every terrain
        int[] CreateTriangles()
        {
            int[] triangles = new int[quadsPerChunk * quadsPerChunk * 6];

            for (int q = 0, t = 0, y = 0; y < quadsPerChunk; y++) {
                for (int x = 0; x < quadsPerChunk; x++) {
                    // Order matters!
                    triangles[t++] = q + 0;
                    triangles[t++] = q + quadsPerChunk + 1;
                    triangles[t++] = q + 1;
                    triangles[t++] = q + 1;
                    triangles[t++] = q + quadsPerChunk + 1;
                    triangles[t++] = q + quadsPerChunk + 2;

                    q++;
                }
                q++;
            }

            return triangles;
        }

        // Create the gradient
        Gradient CreateGradient()
        {
            Gradient gradient = new Gradient();

            // Add colors to colorKey
            colorKey = new GradientColorKey[land.Length + 1];
            colorKey[0].color = water;
            colorKey[0].time = waterLevel;

            for (int i = 0; i < land.Length; i++) {
                colorKey[i + 1].color = land[i];
                colorKey[i + 1].time = waterLevel + i * landStep;
            }

            // Add alphas to alphaKey
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 1.0f;

            gradient.SetKeys(colorKey, alphaKey);

            return gradient;
        }

        // Create a new terrain chunk at grid coordinates x, y
        GameObject GenerateChunk(int x, int y)
        {
            // Set offset of chunk to be generated
            currentChunkOffset = new Vector2(x * chunkSize, y * chunkSize);

            // Copy the Terrain Type
            terrainChunks[x,y] = Instantiate(TerrainType, this.transform.position, this.transform.rotation, this.transform);
            
            return terrainChunks[x, y];
        }

        // Set the size of the map and chunk in units
        void SetSizes()
        {
            chunkSize = quadsPerChunk * quadSize;
            mapSize = chunkSize * maxGridSize;
        }

        void Awake()
        {
            SetSizes();
            terrainChunks = new GameObject[maxGridSize, maxGridSize];
            gradient = CreateGradient();
            triangles = CreateTriangles();
            mapRenderController = GetComponent<MapRenderController>();
        }

        void OnValidate()
        {
            // Max Grid Size
            maxGridSize = maxGridSize / 2 * 2;

            // Disallow more than 7 colors for land
            if (land.Length > 7)
                System.Array.Resize(ref land, 7);

            SetSizes();
        }

        void Update()
        {
            // Delete all chunks if space is pressed
            if (Input.GetKey(KeyCode.Space))
                DeleteAllChunks();

            float mod = 1f;
            if (Input.GetKey(KeyCode.LeftControl))
                mod = -1f;
            if (Input.GetMouseButton(0))
                ChangeHeight(mapRenderController.mousePosition, 0.01f * mod);
        }
    }
}
