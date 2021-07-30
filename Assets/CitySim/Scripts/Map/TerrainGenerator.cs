using UnityEngine;

namespace Unity.CitySim.Map
{
    [RequireComponent(typeof(MeshFilter))]
    public class TerrainGenerator : MonoBehaviour
    {

        Vector2 offset; // Offset from world origo
        Vector2 perlinOffset; // Offset for Perlin noise
        int size; // Size of terrain chunk in quads
        float lod; // Level Of Detail, size of quad
        int octaves; // Number of layers of Perlin noise
        float initialAmplitude; // Initial amplitude for Perlin noise
        float persistance; // Amplitude modifier (Overall height of terrain)
        float initialFrequency; // Initial frequency for Perlin noise
        float lacunarity; // Frequency modifier (Density of peaks)

        Mesh mesh;
        Vector3[] vertices;
        Color[] colors;
        int[] triangles;
        MapGenerator mapGenerator;

        public void SetOffset(Vector2 offset)
        {
            this.offset = offset;
        }

        void GenerateTerrain()
        {

            // Loop through all points in terrain and generate a height and color for each point
            for (int i = 0, y = 0; y <= size; y++) {
                for (int x = 0; x <= size; x++) {
                    // Height
                    float height = GenerateHeight(x, y);
                    vertices[i] = new Vector3(lod * x, height, lod * y);

                    // Color
                    colors[i++] = mapGenerator.gradient.Evaluate(
                        Mathf.InverseLerp(0, mapGenerator.initialAmplitude, height)
                    );
                }
            }

            // Array for triangle corners
            triangles = new int[size * size * 6];
            
            // Connect verticies to create triangles
            for (int q = 0, t = 0, y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    // Order matters!
                    triangles[t++] = q + 0;
                    triangles[t++] = q + size + 1;
                    triangles[t++] = q + 1;
                    triangles[t++] = q + 1;
                    triangles[t++] = q + size + 1;
                    triangles[t++] = q + size + 2;

                    q++;
                }
                q++;
            }
        }

        // Algorithm for generating the height based on x, y and offset using Perlin noise
        float GenerateHeight(int x, int y)
        {
            float amplitude = initialAmplitude;
            float frequency = initialFrequency;
            float noise = 0;

            for (int i = 0; i < octaves; i++) {
                float perlinX = (offset.x + x * lod) / size * frequency;
                float perlinY = (offset.y + y * lod) / size * frequency;
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
            if (vertices == null)
                return 0f;

            // Get x, y coordinates among vertices
            Vector2Int vertex = new Vector2Int(
                (int)(position.x / lod),
                (int)(position.y / lod)
            );

            // Index in vertices array
            vertex.x = Mathf.Clamp(vertex.x, 0, size);
            vertex.y = Mathf.Clamp(vertex.y, 0, size);
            int i = vertex.x + vertex.y * (size + 1);

            // Get local coordinates within quad (0 to 1)
            float x = position.x / lod - vertex.x;
            float y = position.y / lod - vertex.y;
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

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
        }

        void Awake()
        {
            mapGenerator = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>();
            
            this.size = mapGenerator.chunkSize;
            this.lod = mapGenerator.levelOfDetail;
            this.perlinOffset = mapGenerator.perlinOffset;
            this.octaves = mapGenerator.octaves;
            this.initialAmplitude = mapGenerator.initialAmplitude;
            this.persistance = mapGenerator.persistance;
            this.initialFrequency = mapGenerator.initialFrequency;
            this.lacunarity = mapGenerator.lacunarity;
        }

        void Start()
        {
            // Arrays for all points in terrain (height and color)
            vertices = new Vector3[(size + 1) * (size + 1)];
            colors = new Color[vertices.Length];

            if (offset == null) {
                Debug.LogError("Offset not defined for terrain");
                return;
            }

            // Create the mesh object
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;

            // Generate terrain and mesh
            GenerateTerrain();
            transform.Translate(offset.x, 0, offset.y);
            UpdateMesh();
        }
    }
}
