using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CitySim.Map
{
    public class MapChunkController : MonoBehaviour
    {
        [Range(100, 10000)]
        public int renderRange = 5000;
        MapGenerator mapGenerator;
        GameObject cameraTarget;
        int gridSize;

        public Terrain TerrainAt(Vector3 position)
        {
            // Get the position in grid coordinates
            Vector2Int pos = GetGridCoords(position.x, position.z);

            GameObject terrain = mapGenerator.GetChunk(pos.x, pos.y);

            return terrain.GetComponent<Terrain>();
        }

        void ToggleTerrainActivity()
        {
            Vector2 pos = new Vector2(cameraTarget.transform.position.x, cameraTarget.transform.position.z);

            // Get the start coordinates
            Vector2Int from = GetGridCoords(pos.x - renderRange, pos.y - renderRange);

            // Get the end coordinates
            Vector2Int to = GetGridCoords(pos.x + renderRange, pos.y + renderRange);

            // Deactivate all terrains
            for (int x = 0; x < gridSize; x++) {
                for (int y = 0; y < gridSize; y++) {
                    GameObject chunk = mapGenerator.GetComponent<MapGenerator>().GetChunkIfExist(x, y);
                    if (chunk)
                        chunk.SetActive(false);
                }
            }

            // Activate all terrains within render distance
            for (int x = from.x; x <= to.x; x++) {
                for (int y = from.y; y <= to.y; y++) {
                    mapGenerator.GetComponent<MapGenerator>().GetChunk(x, y).SetActive(true);
                }
            }
        }

        Vector2Int GetGridCoords(float x, float y)
        {
            int xInt, yInt;

            // Translate the world space coordinates to grid coordinates
            if (x < 0)
                xInt = (int)(x / mapGenerator.chunkSize - 1) + (gridSize / 2);
            else
                xInt = (int)(x / mapGenerator.chunkSize) + (gridSize / 2);
            if (y < 0)
                yInt = (int)(y / mapGenerator.chunkSize - 1) + (gridSize / 2);
            else
                yInt = (int)(y / mapGenerator.chunkSize) + (gridSize / 2);

            // Clamp coordinates to be within limits
            xInt = Math.Max(xInt, 0);
            xInt = Math.Min(xInt, gridSize - 1);
            yInt = Math.Max(yInt, 0);
            yInt = Math.Min(yInt, gridSize - 1);

            return new Vector2Int(xInt, yInt);
        }

        void Awake()
        {
            mapGenerator = GetComponent<MapGenerator>();
            cameraTarget = GameObject.FindGameObjectWithTag("Player");
            gridSize = mapGenerator.maxGridSize;
        }

        void Start()
        {

        }

        void Update()
        {
            ToggleTerrainActivity();
        }
    }
}

