using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using System.Linq;

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
        static float waterLevel; // Water level in units
        static MapGenerator mapGenerator;

        Mesh mesh;
        Vector3[] vertices;
        Color[] colors;
        GenerateTerrainJob terrainJob;
        JobHandle terrainHandle;
        bool terrainJobGuard = false;

        struct GenerateTerrainJob : IJob
        {
            public NativeArray<Vector3> vertices;
            public NativeArray<Color> colors;
            public Vector2 offset;

            public GenerateTerrainJob(Vector2 offset)
            {
                int arrayLength = (size + 1) * (size + 1);
                vertices = new NativeArray<Vector3>(arrayLength, Allocator.Persistent);
                colors = new NativeArray<Color>(arrayLength, Allocator.Persistent);
                this.offset = offset;
            }

            public void Execute()
            {
                // Loop through all points in terrain and generate a height for each point
                for (int i = 0, y = 0; y <= size; y++)
                    for (int x = 0; x <= size; x++)
                        vertices[i++] = new Vector3(quadSize * x, GenerateHeight(offset, x, y), quadSize * y);

                Vector3[] verticesArr = vertices.ToArray();
                // Loop through all points in terrain and generate a color for each point
                for (int i = 0, y = 0; y <= size; y++)
                    for (int x = 0; x <= size; x++)
                        colors[i] = GenerateColor(x, y, i++, ref verticesArr);
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

            // Make highlands hillier and lowlands flatter
            float relative = noise / initialAmplitude + mapGenerator.lowlandThreshold;
            relative = Mathf.Pow(relative, mapGenerator.highlandExtremity);
            noise = initialAmplitude * relative;

            // Limit the lowest point to water level
            noise = Mathf.Max(noise, waterLevel);
            
            return noise;
        }

        // Generate color based on neighboring vertices
        static Color GenerateColor(int x, int y, int i, ref Vector3[] vertices)
        {
            float height = vertices[i].y;
            Color soilColor = mapGenerator.soilGradient.Evaluate(Mathf.InverseLerp(0, initialAmplitude, height));

            // Don't add flora to water
            if (height == waterLevel)
                return soilColor;

            Color floraColor = mapGenerator.floraGradient.Evaluate(Mathf.InverseLerp(0, initialAmplitude, height));

            List<float> heights = new List<float>();
            heights.Add(height);

            // Not left edge
            if (x != 0)
                heights.Add(vertices[i - 1].y);
            // Not right edge
            if (x != size)
                heights.Add(vertices[i + 1].y);
            // Not lower edge
            if (y != 0)
                heights.Add(vertices[i - size - 1].y);
            // Not upper edge
            if (y != size)
                heights.Add(vertices[i + size + 1].y);

            // Calculate gradient
            float gradient = heights.Max() - heights.Min();
            gradient = Mathf.Min(gradient, mapGenerator.floraExtremity);
            
            return Color.Lerp(floraColor, soilColor, gradient / mapGenerator.floraExtremity);
        }

        // Get the height of the terrain from a local position, prioritize performance!
        public float HeightAt(float xIn, float yIn)
        {
            // Ensure vertices exist and can be accessed
            if (vertices == null)
                return 0f;

            // Get x, y coordinates among vertices
            Vector2Int vertex = new Vector2Int(
                (int)(xIn / quadSize),
                (int)(yIn / quadSize)
            );

            // Index in vertices array
            int i = vertex.x + vertex.y * (size + 1);

            // Get local coordinates within quad (0 to 1)
            float x = xIn / quadSize - vertex.x;
            float y = yIn / quadSize - vertex.y;
            float xi = 1f - x;
            float yi = 1f - y;

            // Heights of the 4 vertices where A is 'vertex', like
            // B---C
            // | \ |
            // A---D
            float A = vertices[i].y;
            float B = vertices[i + size + 1].y;
            float C = vertices[i + size + 2].y;
            float D = vertices[i + 1].y;

            // Avoid division by 0
            if (x + y == 0)
                return A;
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

        // Get the position of the 3 vertices that make up the triangle
        public Vector3[] TriangleCorners(float x, float y)
        {
            // Ensure vertices exist and can be accessed
            if (vertices == null)
                return null;

            int i = ToIndex(x, y);
            Vector3 off = new Vector3(offset.x, 0f, offset.y);

            if (InLowerLeft(x, y))
                return new Vector3[] {
                    vertices[i] + off,
                    vertices[i + size + 1] + off,
                    vertices[i + 1] + off
                };
            else
                return new Vector3[] {
                    vertices[i + size + 1] + off,
                    vertices[i + size + 2] + off,
                    vertices[i + 1] + off
                };
        }

        // Change the height of all surrounding verticies by 'change'
        // If x or y is negative, the height change was done in a neighbor chunk (along the edge)
        public float ChangeHeight(float x, float y, float change, bool level)
        {
            // Ensure vertices exist and can be accessed
            if (vertices == null)
                return -1f;

            // Get the original values of x, y
            float origX = x < 0f ? x + size * quadSize : x;
            float origY = y < 0f ? y + size * quadSize : y;
            
            bool lowerLeft = InLowerLeft(origX, origY);
            int index = ToIndex(origX, origY);
            List<int> indices = new List<int>();

            // If the height change was made in this chunk
            if (x >= 0f && y >= 0f) {
                indices.Add(index + 1);
                indices.Add(index + size + 1);
                if (lowerLeft)
                    indices.Add(index);
                else
                    indices.Add(index + size + 2);
            }
            // If the height change was made in a corner neighbor
            else if (x < 0f && y < 0f) {
                // Right edge
                if (origX < quadSize) {
                    // Lower corner
                    if (origY >= quadSize)
                        indices.Add(size);
                    // Upper corner
                    else if (lowerLeft)
                        indices.Add(vertices.Length - 1);
                }
                // Left edge
                else {
                    // Upper corner
                    if (origY < quadSize)
                        indices.Add(vertices.Length - size - 1);
                    // Lower corner
                    else if (!lowerLeft)
                        indices.Add(0);
                }
            }
            // If the height change was made in a horizontal neighbor
            else if (x < 0f) {
                // Right edge
                if (origX < quadSize) {
                    // Upper vertex (always affected)
                    indices.Add(index + 2 * size + 1);
                    // Lower vertex
                    if (lowerLeft)
                        indices.Add(index + size);
                }
                // Left edge
                else {
                    // Lower vertex (always affected)
                    indices.Add(index - size + 1);
                    // Upper vertex
                    if (!lowerLeft)
                        indices.Add(index + 2);
                }
            }
            // If the height change was made in a vertical neighbor
            else if (y < 0f) {
                // Upper edge
                if (origY < quadSize) {
                    // Right vertex (always affected)
                    indices.Add(index + size * (size + 1) + 1);
                    // Left vertex
                    if (lowerLeft)
                        indices.Add(index + size * (size + 1));
                }
                // Lower edge
                else {
                    // Left vertex (always affected)
                    indices.Add(index % (size + 1));
                    // Right vertex
                    if (!lowerLeft)
                        indices.Add(index % (size + 1) + 1);
                }
            } else {
                Debug.LogError("Unexpected mathematical error in TerrainGenerator/ChangeHeight()");
            }

            // Calculate average height if needed
            float averageHeight = 0f;
            if (level && indices.Count == 3) {
                for (int i = 0; i < 3; i++)
                    averageHeight += vertices[indices[i]].y;
                averageHeight /= 3f;
            } else if (level)
                averageHeight = change;

            // Change the height and color at gathered indices
            for (int j = 0; j < indices.Count; j++) {
                int i = indices[j];
                float height = level ? averageHeight : vertices[i].y + change;
                height = Mathf.Max(height, waterLevel);
                vertices[i].y = height;
                colors[i] = GenerateColor(i % (size + 1), i / (size + 1), i, ref vertices);
            }

            UpdateMesh();

            return averageHeight;
        }

        // Converts world position to grid coordinates
        Vector2Int ToGridCoords(float x, float y)
        {
            int xInt, yInt;

            // Translate the chunk coordinates to grid coordinates
            xInt = (int)(x / (quadSize));
            yInt = (int)(y / (quadSize));

            // Clamp coordinates to be within limits
            xInt = Mathf.Clamp(xInt, 0, size - 1);
            yInt = Mathf.Clamp(yInt, 0, size - 1);

            return new Vector2Int(xInt, yInt);
        }

        // Converts world position to coordinates within quad (0 until 1)
        bool InLowerLeft(float x, float y)
        {
            Vector2Int gridCoords = ToGridCoords(x, y);
            Vector2 quadCoords = new Vector2(x / quadSize - gridCoords.x, y / quadSize - gridCoords.y);
            return quadCoords.x + quadCoords.y < 1f;
        }

        // Converts world position to index of the vertex to the left and below
        int ToIndex(float x, float y)
        {
            Vector2Int gridCoords = ToGridCoords(x, y);
            return gridCoords.x + gridCoords.y * (size + 1);
        }

        // This function should be called only after GenerateTerrainJob has completed
        void GenerateTerrainCompleted()
        {
            vertices = terrainJob.vertices.ToArray();
            colors = terrainJob.colors.ToArray();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = mapGenerator.triangles;

            GetComponent<MeshFilter>().mesh = mesh;
        }

        void UpdateMesh()
        {
            mesh.vertices = vertices;
            mesh.colors = colors;
        }

        void Awake()
        {
            mapGenerator = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>();
            
            // Get values from map generator
            size = mapGenerator.quadsPerChunk;
            quadSize = mapGenerator.quadSize;
            perlinOffset = mapGenerator.perlinOffset;
            octaves = mapGenerator.octaves;
            initialAmplitude = mapGenerator.initialAmplitude;
            persistance = mapGenerator.persistance;
            initialFrequency = mapGenerator.initialFrequency;
            lacunarity = mapGenerator.lacunarity;
            offset = mapGenerator.currentChunkOffset;
            waterLevel = mapGenerator.waterLevel * initialAmplitude - 0.001f;
            mesh = new Mesh();

            // Move object to correct location
            transform.Translate(offset.x, 0, offset.y);

            // Create the terrain generation job
            terrainJob = new GenerateTerrainJob(offset);
            terrainHandle = terrainJob.Schedule();
            terrainJobGuard = true;
        }

        void Update()
        {
            // Wait for terrain generation job to finish
            if (terrainJobGuard && terrainHandle.IsCompleted) {
                terrainJobGuard = false;
                terrainHandle.Complete();
                GenerateTerrainCompleted();
            }
        }

        void OnDestroy()
        {
            // Free up memory
            terrainHandle.Complete();
            terrainJob.vertices.Dispose();
            terrainJob.colors.Dispose();
        }
    }
}
