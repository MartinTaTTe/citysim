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

        [Range(1, 10)]
        public int highlandExtremity = 3;

        [Range(0f, 0.5f)]
        public float lowlandThreshold = 0.2f;

        [Header("")]
        [Range(0f, 0.3f)]
        public float waterLevel = 0.1f;
        public Color water;

        [Range(0f, 0.2f)]
        public float zoneStep = 0.1f;
        [Range(0f, 0.01f)]
        public float beachZone = 0.05f;

        [Range(1f, 10f)]
        public float floraExtremity = 3f;
        public Color[] soil;
        public Color[] flora;
        public Gradient soilGradient { get; private set; }
        public Gradient floraGradient { get; private set; }
        public GameObject terrainType;
        public MapRenderController mapRenderController;
        public float mapSize;
        public float chunkSize;
        public int[] triangles { get; private set; }
        public Vector2 currentChunkOffset { get; private set; }

        GameObject[,] terrainChunks;
        GradientColorKey[] colorKey;
        GradientAlphaKey[] alphaKey;

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

        // Get the position of the 3 vertices that make up the triangle
        public Vector3[] TriangleCorners(float x, float y)
        {
            // Ensure position is within map borders
            if (x < 0f || y < 0f || x >= mapSize || y >= mapSize)
                return null;

            // Get the position in grid coordinates
            Vector2Int gridPos = ToGridCoords(x, y);

            // Get the chunk where we are
            GameObject terrain = GetChunk(gridPos.x, gridPos.y);

            // If height requested in undefined chunk
            if (terrain == null)
                return null;
                
            // Get the offset within the chunk
            float xOff = x - (gridPos.x * chunkSize);
            float yOff = y - (gridPos.y * chunkSize);

            return terrain.GetComponent<TerrainGenerator>().TriangleCorners(xOff, yOff);
        }

        public Vector3[] TriangleCorners(Vector3 position)
        {
            return TriangleCorners(position.x, position.z);
        }

        // Change the height of all surrounding verticies by 'change'
        public void ChangeHeight(float x, float y, float change, bool level = false)
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
            
            float height = terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff, yOff, change, level);

            // Ensure height change was successful
            if (height == -1f)
                return;

            // Use the return value if we are leveling the terrain
            height = level ? height : change;
            
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
                    terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff - chunkSize, yOff, height, level);
            }
            if (neighborOffset.y != 0) {
                terrain = GetChunk(gridPos.x, gridPos.y + neighborOffset.y);
                if (terrain)
                    terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff, yOff - chunkSize, height, level);
            }
            if (neighborOffset.x != 0 && neighborOffset.y != 0) {
                terrain = GetChunk(gridPos.x + neighborOffset.x, gridPos.y + neighborOffset.y);
                if (terrain)
                    terrain.GetComponent<TerrainGenerator>().ChangeHeight(xOff - chunkSize, yOff - chunkSize, height, level);
            }
        }

        public void ChangeHeight(Vector3 position, float change, bool level = false)
        {
            if (mapRenderController.mousePosition.y != -1)
                ChangeHeight(position.x, position.z, change, level);
        }

        // Delete all chunks in order to regenerate them
        public void DeleteAllChunks()
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

        // Create gradient based on the colors array
        Gradient CreateGradient(Color[] colors, bool beach)
        {
            Gradient gradient = new Gradient();

            // Add colors to colorKey
            colorKey = new GradientColorKey[colors.Length + 1];

            // If the gradient should have a dedicated beach zone
            if (beach) {
                colorKey[0].color = colors[0];
                colorKey[0].time = waterLevel + beachZone;
                for (int i = 1; i < colors.Length; i++) {
                    colorKey[i].color = colors[i];
                    colorKey[i].time = waterLevel + beachZone + (i - 1) * zoneStep;
                }
            } else {
                colorKey[0].color = water;
                colorKey[0].time = waterLevel;
                for (int i = 0; i < colors.Length; i++) {
                    colorKey[i + 1].color = colors[i];
                    colorKey[i + 1].time = waterLevel + i * zoneStep;
                }
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
            terrainChunks[x,y] = Instantiate(terrainType, transform.position, transform.rotation, transform);
            
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
            soilGradient = CreateGradient(soil, false);
            floraGradient = CreateGradient(flora, true);
            triangles = CreateTriangles();
        }

        void OnValidate()
        {
            // Max Grid Size
            maxGridSize = maxGridSize / 2 * 2;

            // Disallow more than 7 colors for soil and flora
            if (soil.Length > 7)
                System.Array.Resize(ref soil, 7);
            if (flora.Length > 7)
                System.Array.Resize(ref flora, 7);

            SetSizes();
        }
    }
}
