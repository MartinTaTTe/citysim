using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CitySim.TerrainGenerator
{
    [RequireComponent(typeof(Terrain))]
    public class TerrainGenerator : MonoBehaviour
    {
        [Range(10, 500)]
        public int height = 100;

        [Range(65, 4097)]
        public int size = 10;

        [Range(1, 10)]
        public float intensity = 5f;

        public Vector2 offset;
        Terrain terrain;

        public TerrainGenerator(Vector2 offset)
        {
            this.offset = offset;
        }

        void GenerateTerrain()
        {
            // 2D-array for all points in terrain
            float[,] heights = new float[size, size];

            // Loop through all points in terrain and generate a height for each point
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    heights[x, y] = GenerateHeight(x, y);
                }
            }

            // Assign size and heights to TerrainData
            terrain.terrainData.heightmapResolution = size + 1;
            terrain.terrainData.size = new Vector3(size, height, size);
            terrain.terrainData.SetHeights(0, 0, heights);
        }

        // Algorithm for generating the height based on x, y and offset using Perlin noise
        float GenerateHeight(int x, int y)
        {
            float noise = Mathf.PerlinNoise(
                        (offset.x + x) / size  * intensity,
                        (offset.y + y) / size  * intensity
                );
            return noise;
        }

        void Start()
        {
            terrain = GetComponent<Terrain>();
            GenerateTerrain();
        }

        void OnValidate()
        {
            if (size <= 65)
                size = 65;
            else if (size <= 129)
                size = 129;
            else if (size <= 257)
                size = 257;
            else if (size <= 513)
                size = 513;
            else if (size <= 1025)
                size = 1025;
            else if (size <= 2049)
                size = 2049;
            else
                size = 4097;
        }
    }
}
