using UnityEngine;

namespace Unity.CitySim.Map
{
    [RequireComponent(typeof(MeshFilter))]
    public class TerrainGenerator : MonoBehaviour
    {
        public Vector2 offset; // Offset from world origo
        Vector2 perlinOffset; // Offset for Perlin noise
        int size; // Size of terrain chunk in quads
        float lod; // Level Of Detail, size of quad
        float intensity; // Intenisity parameter for Perlin noise
        int maxHeight; // Maximum height of terrain

        Mesh mesh;
        Vector3[] vertices;
        int[] triangles;
        MapGenerator mapGenerator;

        public void SetOffset(Vector2 offset)
        {
            this.offset = offset;
        }

        void GenerateTerrain()
        {

            // Loop through all points in terrain and generate a height for each point
            for (int i = 0, y = 0; y <= size; y++) {
                for (int x = 0; x <= size; x++) {
                    vertices[i++] = new Vector3(lod * x, GenerateHeight(x, y), lod * y);
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
            float perlinX = (offset.x + x * lod) / size * intensity;
            float perlinY = (offset.y + y * lod) / size * intensity;
            float noise = Mathf.PerlinNoise(perlinX + perlinOffset.x, perlinY + perlinOffset.y);
            
            return noise * maxHeight;
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
            int i = vertex.x + vertex.y * (size + 1);
            i = Mathf.Clamp(i, 0, vertices.Length - 1);

            // Get local coordinates within quad (0 to 1)
            float x = position.x / lod - vertex.x;
            float y = position.y / lod - vertex.y;
            float xi = 1 - x;
            float yi = 1 - y;

            // Whether or not the point is in the lower left triangle or the upper right triangle
            bool inLowerLeft = x + y < 1f;

            // Heights of the 4 vertices where C is 'vertex', like
            // A---B
            // | \ |
            // C---D
            float A = vertices[i + size + 1].y;
            float B = vertices[i + size + 2].y;
            float C = vertices[i].y;
            float D = vertices[i + 1].y;

            // Avoid division by 0
            if (x + y == 0 || xi + yi == 0)
                return C;

            // Lower left triangle in the quad
            if (inLowerLeft) {
                // Lerp from A to D
                float AToD = x + (x / (x + y)) * (1f - x - y);
                float X = Mathf.Lerp(A, D, AToD);

                // Lerp from the new midpoint to C
                float CToX = x + y;
                return Mathf.Lerp(C, X, CToX);
            }
            // Upper right triangle in the quad
            else {
                // Lerp from A to D
                float AToD = yi + (yi / (xi + yi)) * (1f - xi - yi);
                float X = Mathf.Lerp(A, D, AToD);

                // Lerp from the new midpoint to B
                float BToX = xi + yi;
                return Mathf.Lerp(B, X, BToX);
            }
        }

        void UpdateMesh()
        {
            mesh.Clear();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
        }

        void Awake()
        {
            mapGenerator = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>();
            
            this.perlinOffset = mapGenerator.perlinOffset;
            this.size = mapGenerator.chunkSize;
            this.lod = mapGenerator.levelOfDetail;
            this.intensity = mapGenerator.intensity;
            this.maxHeight = mapGenerator.maxHeight;
        }

        void Start()
        {
            // Array for all points in terrain
            vertices = new Vector3[(size + 1) * (size + 1)];

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
