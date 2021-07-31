using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.CitySim.Map
{
    [RequireComponent(typeof(MeshFilter))]
    public class TerrainGenerator : MonoBehaviour
    {

        Vector2 offset; // Offset from world origo
        static Vector2 perlinOffset; // Offset for Perlin noise
        static int size; // Size of terrain chunk in quads
        static float quadSize; // Level Of Detail, size of quad
        static int octaves; // Number of layers of Perlin noise
        static float initialAmplitude; // Initial amplitude for Perlin noise
        static float persistance; // Amplitude modifier (Overall height of terrain)
        static float initialFrequency; // Initial frequency for Perlin noise
        static float lacunarity; // Frequency modifier (Density of peaks)

        Mesh mesh;
        NativeArray<Vector3> vertices;
        NativeArray<Color> colors;
        static MapGenerator mapGenerator;

        public void SetOffset(Vector2 offset)
        {
            this.offset = offset;
        }

        struct GenerateTerrainJob : IJob
        {
            public Vector2 offset;
            public NativeArray<Vector3> vertices;
            public NativeArray<Color> colors;

            public void Execute()
            {
                // Loop through all points in terrain and generate a height and color for each point
                for (int i = 0, y = 0; y <= size; y++) {
                    for (int x = 0; x <= size; x++) {
                        // Height
                        float height = GenerateHeight(offset, x, y);
                        vertices[i] = new Vector3(quadSize * x, height, quadSize * y);

                        // Color
                        colors[i++] = mapGenerator.gradient.Evaluate(
                            Mathf.InverseLerp(0, mapGenerator.initialAmplitude, height)
                        );
                    }
                }
            }
        }

        // Algorithm for generating the height based on x, y and offset using Perlin noise
        static float GenerateHeight(Vector2 offset, int x, int y)
        {
            float amplitude = initialAmplitude;
            float frequency = initialFrequency;
            float noise = 0;

            for (int i = 0; i < octaves; i++) {
                float perlinX = (offset.x + x * quadSize) / size * frequency;
                float perlinY = (offset.y + y * quadSize) / size * frequency;
                float perlin = Mathf.PerlinNoise(perlinX + perlinOffset.x, perlinY + perlinOffset.y);
                noise += perlin * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }
            
            return noise;
        }

        // Get the height of the terrain from a local position
        public float HeightAt(Vector2 position)
        {
            if (!terrainHandle.IsCompleted || vertices == null)
                return 0f;

            // Get x, y coordinates among vertices
            Vector2Int vertex = new Vector2Int(
                (int)(position.x / quadSize),
                (int)(position.y / quadSize)
            );

            // Index in vertices array
            vertex.x = Mathf.Clamp(vertex.x, 0, size);
            vertex.y = Mathf.Clamp(vertex.y, 0, size);
            int i = vertex.x + vertex.y * (size + 1);

            // Get local coordinates within quad (0 to 1)
            float x = position.x / quadSize - vertex.x;
            float y = position.y / quadSize - vertex.y;
            float xi = 1 - x;
            float yi = 1 - y;

            // Heights of the 4 vertices where A is 'vertex', like
            // B---C
            // | \ |
            // A---D
            float A = vertices[i].y;
            float B;
            float C;
            float D;

            // Avoid division by 0
            if (x + y == 0)
                return A;

            // Between A and B
            if (x == 0) {
                B = vertices[i + size + 1].y;
                return Mathf.Lerp(A, B, y);
            }

            // Between A and D
            if (y == 0) {
                D = vertices[i + 1].y;
                return Mathf.Lerp(A, D, x);
            }

            B = vertices[i + size + 1].y;
            C = vertices[i + size + 2].y;
            D = vertices[i + 1].y;

            // Avoid division by 0
            if (xi + yi == 0)
                return C;

            // Whether the point is in the lower left triangle ...
            if (x + y < 1f) {
                // Lerp from A to D
                float BToD = Mathf.Lerp(x, yi, x / (x + y));
                float X = Mathf.Lerp(B, D, BToD);

                // Lerp from the new midpoint to C
                float AToX = x + y;
                return Mathf.Lerp(A, X, AToX);
            }
            // ... or in the upper right triangle in the quad
            else {
                // Lerp from A to D
                float BToD = Mathf.Lerp(yi, x, yi / (xi + yi));
                float X = Mathf.Lerp(B, D, BToD);

                // Lerp from the new midpoint to B
                float CToX = xi + yi;
                return Mathf.Lerp(C, X, CToX);
            }
        }

        void UpdateMesh()
        {
            mesh.Clear();

            mesh.vertices = vertices.ToArray();
            mesh.colors = colors.ToArray();
            mesh.triangles = mapGenerator.triangles;
        }

        void Awake()
        {
            mapGenerator = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>();
            
            size = mapGenerator.chunkSize;
            quadSize = mapGenerator.quadSize;
            perlinOffset = mapGenerator.perlinOffset;
            octaves = mapGenerator.octaves;
            initialAmplitude = mapGenerator.initialAmplitude;
            persistance = mapGenerator.persistance;
            initialFrequency = mapGenerator.initialFrequency;
            lacunarity = mapGenerator.lacunarity;

            // Arrays for all points in terrain (height and color)
            int arrayLength = (size + 1) * (size + 1);
            vertices = new NativeArray<Vector3>(arrayLength, Allocator.Persistent);
            colors = new NativeArray<Color>(arrayLength, Allocator.Persistent);
        }

        bool terrainJobGuard = true;
        JobHandle terrainHandle;
        void Start()
        {
            if (offset == null) {
                Debug.LogError("Offset not defined for terrain");
                return;
            }

            // Create the mesh object
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
            transform.Translate(offset.x, 0, offset.y);

            // Set up the terrain generation job
            var terrainJob = new GenerateTerrainJob();
            terrainJob.offset = offset;
            terrainJob.vertices = vertices;
            terrainJob.colors = colors;

            // Start the terrain generation job
            terrainHandle = terrainJob.Schedule();
        }

        void Update()
        {
            // Wait for terrain generation job to finish
            if (terrainJobGuard && terrainHandle.IsCompleted) {
                terrainJobGuard = false;
                terrainHandle.Complete();
                UpdateMesh();
            }
        }

        void OnDestroy()
        {
            // Free up memory
            terrainHandle.Complete();
            vertices.Dispose();
            colors.Dispose();
        }
    }
}
