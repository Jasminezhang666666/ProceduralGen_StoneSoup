using System.Collections.Generic;
using UnityEngine;

public class jz5144TrapMoveableRoom : Room
{
    [Header("Spawn Counts")]
    [Range(0, 3)] public int maxTraps = 3;

    [Range(0f, 1f)] public float chanceToHaveMoveableWalls = 0.55f; // 0 or 2-3
    public int minMoveableWalls = 2;
    public int maxMoveableWalls = 3;

    [Header("Walls")]
    [Range(0f, 1f)] public float edgeWallChance = 0.65f;
    public int minInteriorWalls = 0;
    public int maxInteriorWalls = 5;

    public override void fillRoom(LevelGenerator ourGenerator, ExitConstraint requiredExits)
    {
        // trap (4)
        // moveable wall (5)
        GameObject trapPrefab = (localTilePrefabs != null && localTilePrefabs.Length > 0) ? localTilePrefabs[0] : null;
        GameObject moveableWallPrefab = (localTilePrefabs != null && localTilePrefabs.Length > 1) ? localTilePrefabs[1] : null;

        int w = LevelGenerator.ROOM_WIDTH;
        int h = LevelGenerator.ROOM_HEIGHT;

        GameObject wallPrefab = null;
        if (ourGenerator != null)
        {
            wallPrefab = ourGenerator.normalWallPrefab != null
                ? ourGenerator.normalWallPrefab
                : (ourGenerator.globalTilePrefabs != null && ourGenerator.globalTilePrefabs.Length > 0 ? ourGenerator.globalTilePrefabs[0] : null);
        }

        int[,] grid = new int[w, h];

        // Outer bounds: random wall or empty
        // corners always wall
        for (int x = 0; x < w; x++)
        {
            grid[x, 0] = (Random.value < edgeWallChance) ? 1 : 0;
            grid[x, h - 1] = (Random.value < edgeWallChance) ? 1 : 0;
        }
        for (int y = 0; y < h; y++)
        {
            grid[0, y] = (Random.value < edgeWallChance) ? 1 : 0;
            grid[w - 1, y] = (Random.value < edgeWallChance) ? 1 : 0;
        }

        grid[0, 0] = 1;
        grid[w - 1, 0] = 1;
        grid[0, h - 1] = 1;
        grid[w - 1, h - 1] = 1;

        // ensure required exits are not blocked by edge randomization
        foreach (var p in requiredExits.requiredExitLocations())
        {
            if (p.x < 0 || p.x >= w || p.y < 0 || p.y >= h) continue;

            grid[p.x, p.y] = 0;

            // carve 1 tile inward so it actually connects inside
            if (p.y == h - 1 && h - 2 >= 0) grid[p.x, h - 2] = 0;
            else if (p.y == 0 && 1 < h) grid[p.x, 1] = 0;
            else if (p.x == 0 && 1 < w) grid[1, p.y] = 0;
            else if (p.x == w - 1 && w - 2 >= 0) grid[w - 2, p.y] = 0; 
        }

        // Scatter a few interior normal walls
        int interiorWallCount = Random.Range(minInteriorWalls, maxInteriorWalls + 1);
        PlaceRandomTiles(grid, interiorWallCount, tileValue: 1, w, h);

        // Moveable walls: 0 OR 2–3
        int moveableCount = 0;
        if (Random.value < chanceToHaveMoveableWalls)
            moveableCount = Random.Range(minMoveableWalls, maxMoveableWalls + 1);
        PlaceRandomTiles(grid, moveableCount, tileValue: 5, w, h);

        // Traps: 0–3
        int trapCount = Random.Range(0, maxTraps + 1);
        PlaceRandomTiles(grid, trapCount, tileValue: 4, w, h);

        // Spawn tiles
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int v = grid[x, y];
                if (v == 0) continue;

                GameObject prefab = null;
                if (v == 1) prefab = wallPrefab;
                else if (v == 4) prefab = trapPrefab;
                else if (v == 5) prefab = moveableWallPrefab;

                if (prefab != null)
                    Tile.spawnTile(prefab, transform, x, y);
            }
        }
    }

    // interior tiles
    private void PlaceRandomTiles(int[,] grid, int count, int tileValue, int w, int h)
    {
        if (count <= 0) return;

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (grid[x, y] != 0) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        // shuffle candidates
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < count; i++)
        {
            var p = candidates[i];
            if (grid[p.x, p.y] != 0) continue;
            grid[p.x, p.y] = tileValue;
            placed++;
        }
    }
}