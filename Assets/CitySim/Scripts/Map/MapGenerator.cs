using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Range(10, 500)]
        public int maxHeight = 100;

        [Range(65, 4097)]
        public int chunkSize = 513;

        [Range(1, 10)]
        public float intensity = 5f;

        [Range(2, 100)]
        public int maxGridSize = 10;

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

        GameObject GenerateChunk(int x, int y)
        {
            // Copy the Terrain Type
            terrainChunks[x,y] = Instantiate(TerrainType, this.transform.position, this.transform.rotation, this.transform);

            // Set the correct values
            terrainChunks[x,y].GetComponent<TerrainGenerator>()
                .Initialize(maxHeight, chunkSize, intensity, new Vector2((x - (maxGridSize / 2)) * chunkSize, (y - (maxGridSize / 2)) * chunkSize));
            
            return terrainChunks[x, y];
        }

        void Awake()
        {
            terrainChunks = new GameObject[maxGridSize, maxGridSize];
        }

        void OnValidate()
        {
            // Chunk Size
            if (chunkSize <= 65)
                chunkSize = 65;
            else if (chunkSize <= 129)
                chunkSize = 129;
            else if (chunkSize <= 257)
                chunkSize = 257;
            else if (chunkSize <= 513)
                chunkSize = 513;
            else if (chunkSize <= 1025)
                chunkSize = 1025;
            else if (chunkSize <= 2049)
                chunkSize = 2049;
            else
                chunkSize = 4097;

            // Max Grid Size
            maxGridSize = maxGridSize / 2 * 2;
        }
    } 
}
