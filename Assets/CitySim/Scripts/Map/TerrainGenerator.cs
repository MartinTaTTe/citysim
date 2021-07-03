using UnityEngine;

namespace Unity.CitySim.Map
{
    [RequireComponent(typeof(MeshFilter))]
    public class TerrainGenerator : MonoBehaviour
    {
        public Vector2 offset; // Offset from world origo
        Vector2 perlinOffset; // Offset for Perlin noise
        int size; // Tile width for terrain chunk
        float lod; // Level Of Detail, size of tile
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
            // Get x, y coordinates among vertices
            int x = (int)(position.x / lod);
            int y = (int)(position.y / lod);

            // Index in vertices array
            int i = x + y * (size + 1);
            i = Mathf.Clamp(i, 0, vertices.Length - 1);

            if (vertices == null)
                return 0f;
            else
                return vertices[i].y;
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
