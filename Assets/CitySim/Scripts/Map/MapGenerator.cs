using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map parameters")]

        [Range(0, 255)]
        public int chunkSize = 250;

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
            xInt = (int)(x / (chunkSize * quadSize));
            yInt = (int)(y / (chunkSize * quadSize));

            // Clamp coordinates to be within limits
            xInt = Mathf.Clamp(xInt, 0, maxGridSize - 1);
            yInt = Mathf.Clamp(yInt, 0, maxGridSize - 1);

            return new Vector2Int(xInt, yInt);
        }
        public Vector2Int ToGridCoords(Vector3 position)
        {
            return ToGridCoords(position.x, position.z);
        }

        // Get the height of the terrain from a world position
        public float HeightAt(float x, float y) {
            // Get the position in grid coordinates
            Vector2Int gridPos = ToGridCoords(x, y);

            // Get the chunk where we are
            GameObject terrain = GetChunk(gridPos.x, gridPos.y);

            // If height requested in undefined chunk
            if (terrain == null)
                return 0f;

            // Get the offset within the chunk
            Vector2 localOffset = new Vector2(
                x - (gridPos.x * chunkSize * quadSize),
                y - (gridPos.y * chunkSize * quadSize)
            );

            return terrain.GetComponent<TerrainGenerator>().HeightAt(localOffset);
        }

        public float HeightAt(Vector3 position)
        {
            return HeightAt(position.x, position.z);
        }

        // Create the triangle array used by every terrain
        int[] CreateTriangles()
        {
            int[] triangles = new int[chunkSize * chunkSize * 6];

            for (int q = 0, t = 0, y = 0; y < chunkSize; y++) {
                for (int x = 0; x < chunkSize; x++) {
                    // Order matters!
                    triangles[t++] = q + 0;
                    triangles[t++] = q + chunkSize + 1;
                    triangles[t++] = q + 1;
                    triangles[t++] = q + 1;
                    triangles[t++] = q + chunkSize + 1;
                    triangles[t++] = q + chunkSize + 2;

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
            currentChunkOffset = new Vector2(x * chunkSize * quadSize, y * chunkSize * quadSize);

            // Copy the Terrain Type
            terrainChunks[x,y] = Instantiate(TerrainType, this.transform.position, this.transform.rotation, this.transform);
            
            return terrainChunks[x, y];
        }

        // Set the size of the map in units
        void SetMapSize()
        {
            mapSize = chunkSize * quadSize * maxGridSize;
        }

        void Awake()
        {
            SetMapSize();
            terrainChunks = new GameObject[maxGridSize, maxGridSize];
            gradient = CreateGradient();
            triangles = CreateTriangles();
        }

        void OnValidate()
        {
            // Max Grid Size
            maxGridSize = maxGridSize / 2 * 2;

            // Disallow more than 7 colors for land
            if (land.Length > 7)
                System.Array.Resize(ref land, 7);

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
