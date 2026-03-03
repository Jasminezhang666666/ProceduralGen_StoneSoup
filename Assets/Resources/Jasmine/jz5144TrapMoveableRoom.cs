using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class jz5144TrapMoveableRoom : Room
{
    [Header("DEBUG")]
    public bool debugLogs = true;
    [Tooltip("Print local/global prefab lists (can be spammy).")]
    public bool debugPrintPrefabLists = true;
    [Tooltip("Print candidate counts for each placement call.")]
    public bool debugPrintCandidates = true;

    [Header("Prefab Mapping (IMPORTANT)")]
    [Tooltip("Your Dragon is currently in Local Tile Prefabs Element 2.")]
    public int dragonPrefabLocalIndex = 2;

    [Tooltip("Optional override. If set, this will be used as Dragon prefab, ignoring indices.")]
    public GameObject dragonPrefabOverride = null;

    [Header("Spawn Counts")]
    [Range(0, 3)] public int maxTraps = 3;

    [Header("Variant: Dragon Trap Room (no moveable walls)")]
    [Range(0f, 1f)] public float chanceToBeDragonTrapRoom = 0.35f;
    [Min(1)] public int dragonCount = 1;               // usually 1
    [Tooltip("If true, Dragon-variant guarantees at least 1 trap.")]
    public bool dragonVariantRequiresTrap = true;

    [Header("Dragon Placement Rule (3x3 empty)")]
    [Tooltip("Dragon must spawn only if its 3x3 area (center+8 neighbors) are all EMPTY(0) and not reserved.")]
    public bool dragonRequires3x3Empty = true;

    [Tooltip("How many random attempts when searching a valid 3x3 empty center.")]
    public int dragonFindAttempts = 300;

    [Header("Moveable Walls (Normal Variant Only)")]
    [Range(0f, 1f)] public float chanceToHaveMoveableWalls = 0.55f; // 0 or 2-3
    public int minMoveableWalls = 2;
    public int maxMoveableWalls = 3;

    [Header("Walls")]
    [Range(0f, 1f)] public float edgeWallChance = 0.65f;
    public int minInteriorWalls = 0;
    public int maxInteriorWalls = 5;

    // Internal tile codes (KEEP: trap=4, moveable=5)
    private const int TILE_EMPTY = 0;
    private const int TILE_WALL = 1;
    private const int TILE_TRAP = 4;
    private const int TILE_MOVEABLE = 5;
    private const int TILE_DRAGON = 6;

    public override void fillRoom(LevelGenerator ourGenerator, ExitConstraint requiredExits)
    {
        // trap prefab: localTilePrefabs[0]
        // moveable wall prefab: localTilePrefabs[1]
        // dragon prefab: localTilePrefabs[dragonPrefabLocalIndex] (your current setup is 2)

        GameObject trapPrefab = (localTilePrefabs != null && localTilePrefabs.Length > 0) ? localTilePrefabs[0] : null;
        GameObject moveableWallPrefab = (localTilePrefabs != null && localTilePrefabs.Length > 1) ? localTilePrefabs[1] : null;

        GameObject dragonPrefabLocal = (localTilePrefabs != null && dragonPrefabLocalIndex >= 0 && localTilePrefabs.Length > dragonPrefabLocalIndex)
            ? localTilePrefabs[dragonPrefabLocalIndex]
            : null;

        // Optional fallback: support a global index-6 convention.
        GameObject dragonPrefabGlobal6 = (ourGenerator != null && ourGenerator.globalTilePrefabs != null && ourGenerator.globalTilePrefabs.Length > 6)
            ? ourGenerator.globalTilePrefabs[6]
            : null;

        // Final selection: override > local[index] > global[6]
        GameObject dragonPrefab = dragonPrefabOverride != null
            ? dragonPrefabOverride
            : (dragonPrefabLocal != null ? dragonPrefabLocal : dragonPrefabGlobal6);

        int w = LevelGenerator.ROOM_WIDTH;
        int h = LevelGenerator.ROOM_HEIGHT;

        GameObject wallPrefab = null;
        if (ourGenerator != null)
        {
            wallPrefab = ourGenerator.normalWallPrefab != null
                ? ourGenerator.normalWallPrefab
                : (ourGenerator.globalTilePrefabs != null && ourGenerator.globalTilePrefabs.Length > 0 ? ourGenerator.globalTilePrefabs[0] : null);
        }

        // Decide variant EARLY so we can log it at the top.
        bool isDragonVariant = (Random.value < chanceToBeDragonTrapRoom);

        // --- DEBUG: what is our dragon prefab source? ---
        if (debugLogs)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[TrapMoveableRoom] fillRoom on '{name}' (instance {GetInstanceID()})");
            sb.AppendLine($"  isDragonVariant={isDragonVariant}  chanceToBeDragonTrapRoom={chanceToBeDragonTrapRoom}");
            sb.AppendLine($"  localTilePrefabs length={(localTilePrefabs == null ? -1 : localTilePrefabs.Length)}");
            sb.AppendLine($"  globalTilePrefabs length={(ourGenerator == null || ourGenerator.globalTilePrefabs == null ? -1 : ourGenerator.globalTilePrefabs.Length)}");
            sb.AppendLine($"  dragonPrefabOverride={PrefabName(dragonPrefabOverride)}");
            sb.AppendLine($"  local[{dragonPrefabLocalIndex}]={PrefabName(dragonPrefabLocal)}  global[6]={PrefabName(dragonPrefabGlobal6)}  => chosen dragonPrefab={PrefabName(dragonPrefab)}");
            sb.AppendLine($"  trapPrefab(local[0])={PrefabName(trapPrefab)}  moveable(local[1])={PrefabName(moveableWallPrefab)}  wallPrefab={PrefabName(wallPrefab)}");
            Debug.Log(sb.ToString(), this);

            if (debugPrintPrefabLists)
            {
                Debug.Log($"  localTilePrefabs dump:\n{DumpPrefabArray(localTilePrefabs)}", this);
                if (ourGenerator != null)
                    Debug.Log($"  globalTilePrefabs dump:\n{DumpPrefabArray(ourGenerator.globalTilePrefabs)}", this);
            }

            if (isDragonVariant && dragonPrefab == null)
            {
                Debug.LogWarning("[TrapMoveableRoom] Dragon variant chosen BUT dragonPrefab is NULL. " +
                                 "Check dragonPrefabOverride, localTilePrefabs[dragonPrefabLocalIndex], or globalTilePrefabs[6].",
                                 this);
            }
        }

        int[,] grid = new int[w, h];

        // Reserve exit-related tiles so we don't place traps/dragons/moveables on them
        HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();

        // Outer bounds: random wall or empty (corners always wall)
        for (int x = 0; x < w; x++)
        {
            grid[x, 0] = (Random.value < edgeWallChance) ? TILE_WALL : TILE_EMPTY;
            grid[x, h - 1] = (Random.value < edgeWallChance) ? TILE_WALL : TILE_EMPTY;
        }
        for (int y = 0; y < h; y++)
        {
            grid[0, y] = (Random.value < edgeWallChance) ? TILE_WALL : TILE_EMPTY;
            grid[w - 1, y] = (Random.value < edgeWallChance) ? TILE_WALL : TILE_EMPTY;
        }

        grid[0, 0] = TILE_WALL;
        grid[w - 1, 0] = TILE_WALL;
        grid[0, h - 1] = TILE_WALL;
        grid[w - 1, h - 1] = TILE_WALL;

        // Ensure required exits are not blocked by edge randomization
        foreach (var p in requiredExits.requiredExitLocations())
        {
            if (p.x < 0 || p.x >= w || p.y < 0 || p.y >= h) continue;

            grid[p.x, p.y] = TILE_EMPTY;
            reserved.Add(new Vector2Int(p.x, p.y));

            // carve 1 tile inward so it actually connects inside
            if (p.y == h - 1 && h - 2 >= 0)
            {
                grid[p.x, h - 2] = TILE_EMPTY;
                reserved.Add(new Vector2Int(p.x, h - 2));
            }
            else if (p.y == 0 && 1 < h)
            {
                grid[p.x, 1] = TILE_EMPTY;
                reserved.Add(new Vector2Int(p.x, 1));
            }
            else if (p.x == 0 && 1 < w)
            {
                grid[1, p.y] = TILE_EMPTY;
                reserved.Add(new Vector2Int(1, p.y));
            }
            else if (p.x == w - 1 && w - 2 >= 0)
            {
                grid[w - 2, p.y] = TILE_EMPTY;
                reserved.Add(new Vector2Int(w - 2, p.y));
            }
        }

        // =========================
        // ✅ DRAGON PLACEMENT FIRST (so we can RESERVE its whole 3x3 area)
        // =========================
        if (isDragonVariant && dragonPrefab != null && dragonCount > 0)
        {
            int placed = PlaceDragons_Require3x3Empty(grid, dragonCount, w, h, reserved, debugTag: "Dragons3x3");
            if (debugLogs && placed < dragonCount)
            {
                Debug.LogWarning($"[TrapMoveableRoom] Wanted dragonCount={dragonCount} but only placed={placed}. " +
                                 $"(3x3-empty rule may be too strict given exits/reserved.)", this);
            }
        }

        // Scatter a few interior normal walls
        int interiorWallCount = Random.Range(minInteriorWalls, maxInteriorWalls + 1);
        PlaceRandomTiles(grid, interiorWallCount, TILE_WALL, w, h, reserved, debugTag: "InteriorWalls");

        // Moveable walls (Normal Variant Only): 0 OR 2–3
        int moveableCount = 0;
        if (!isDragonVariant)
        {
            if (Random.value < chanceToHaveMoveableWalls)
                moveableCount = Random.Range(minMoveableWalls, maxMoveableWalls + 1);
            PlaceRandomTiles(grid, moveableCount, TILE_MOVEABLE, w, h, reserved, debugTag: "Moveables");
        }

        // Traps: 0–3 (Dragon variant can require >= 1)
        int trapMin = (isDragonVariant && dragonVariantRequiresTrap) ? 1 : 0;
        int trapCount = Random.Range(trapMin, maxTraps + 1);
        PlaceRandomTiles(grid, trapCount, TILE_TRAP, w, h, reserved, debugTag: "Traps");

        // DEBUG: count what ended up in grid
        if (debugLogs)
        {
            int walls = CountTiles(grid, TILE_WALL, w, h);
            int traps = CountTiles(grid, TILE_TRAP, w, h);
            int moves = CountTiles(grid, TILE_MOVEABLE, w, h);
            int drags = CountTiles(grid, TILE_DRAGON, w, h);

            Debug.Log($"[TrapMoveableRoom] Grid counts => Wall:{walls} Trap:{traps} Moveable:{moves} Dragon:{drags}", this);

            if (isDragonVariant && dragonPrefab != null && drags == 0)
            {
                Debug.LogWarning("[TrapMoveableRoom] Dragon prefab is NOT null, but grid has 0 Dragon tiles. " +
                                 "Likely no valid 3x3-empty candidates due to exits/reserved or room size.", this);
            }

            if (drags > 0)
            {
                Debug.Log($"[TrapMoveableRoom] Dragon positions:\n{ListPositions(grid, TILE_DRAGON, w, h)}", this);
            }
        }

        // Spawn tiles
        int spawnedDragonCount = 0;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int v = grid[x, y];
                if (v == TILE_EMPTY) continue;

                GameObject prefab = null;
                if (v == TILE_WALL) prefab = wallPrefab;
                else if (v == TILE_TRAP) prefab = trapPrefab;
                else if (v == TILE_MOVEABLE) prefab = moveableWallPrefab;
                else if (v == TILE_DRAGON) prefab = dragonPrefab;

                if (prefab != null)
                {
                    Tile.spawnTile(prefab, transform, x, y);
                    if (v == TILE_DRAGON) spawnedDragonCount++;
                }
                else if (debugLogs && (v == TILE_TRAP || v == TILE_MOVEABLE || v == TILE_DRAGON || v == TILE_WALL))
                {
                    Debug.LogWarning($"[TrapMoveableRoom] Tried to spawn tileValue={v} at ({x},{y}) but prefab was NULL.", this);
                }
            }
        }

        if (debugLogs && isDragonVariant)
        {
            Debug.Log($"[TrapMoveableRoom] Spawn summary => spawnedDragonCount={spawnedDragonCount} chosenDragonPrefab={PrefabName(dragonPrefab)}", this);
        }
    }

    // ============================================================
    // ✅ Dragon must be in a 3x3 EMPTY area (center + 8 neighbors are 0)
    // Also RESERVE the whole 3x3 so nothing else can spawn there later.
    // ============================================================
    private int PlaceDragons_Require3x3Empty(int[,] grid, int count, int w, int h,
                                            HashSet<Vector2Int> reserved = null, string debugTag = "")
    {
        if (count <= 0) return 0;

        int placed = 0;

        for (int k = 0; k < count; k++)
        {
            Vector2Int chosenCenter;
            bool found = TryPickDragonCenter3x3(grid, w, h, reserved, out chosenCenter);

            if (!found)
            {
                if (debugLogs && debugPrintCandidates)
                    Debug.LogWarning($"[PlaceDragons:{debugTag}] No valid 3x3-empty center found for dragon #{k + 1}.", this);
                break;
            }

            // place dragon at center
            grid[chosenCenter.x, chosenCenter.y] = TILE_DRAGON;

            // reserve the whole 3x3 so later walls/traps won't occupy them
            Reserve3x3(reserved, chosenCenter);

            placed++;
        }

        if (debugLogs && debugPrintCandidates)
            Debug.Log($"[PlaceDragons:{debugTag}] placed={placed}/{count}", this);

        return placed;
    }

    private bool TryPickDragonCenter3x3(int[,] grid, int w, int h,
                                       HashSet<Vector2Int> reserved, out Vector2Int center)
    {
        // Dragon needs full 3x3 inside bounds, so center must be [1..w-2] & [1..h-2]
        for (int attempt = 0; attempt < dragonFindAttempts; attempt++)
        {
            int cx = Random.Range(1, w - 1);
            int cy = Random.Range(1, h - 1);

            if (!Is3x3AllEmptyAndNotReserved(grid, cx, cy, w, h, reserved)) continue;

            center = new Vector2Int(cx, cy);
            return true;
        }

        center = new Vector2Int(-1, -1);
        return false;
    }

    private bool Is3x3AllEmptyAndNotReserved(int[,] grid, int cx, int cy, int w, int h, HashSet<Vector2Int> reserved)
    {
        // center must support 3x3
        if (cx <= 0 || cy <= 0 || cx >= w - 1 || cy >= h - 1) return false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;

                // must be empty
                if (grid[x, y] != TILE_EMPTY) return false;

                // and must not be reserved (exits etc.)
                if (reserved != null && reserved.Contains(new Vector2Int(x, y))) return false;
            }
        }
        return true;
    }

    private void Reserve3x3(HashSet<Vector2Int> reserved, Vector2Int center)
    {
        if (reserved == null) return;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                reserved.Add(new Vector2Int(center.x + dx, center.y + dy));
            }
        }
    }

    // interior tiles (generic)
    private void PlaceRandomTiles(int[,] grid, int count, int tileValue, int w, int h,
                                  HashSet<Vector2Int> reserved = null, string debugTag = "")
    {
        if (count <= 0)
        {
            if (debugLogs && debugPrintCandidates)
                Debug.Log($"[PlaceRandomTiles:{debugTag}] tileValue={tileValue} countWanted={count} => skip", this);
            return;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (grid[x, y] != TILE_EMPTY) continue;
                if (reserved != null && reserved.Contains(new Vector2Int(x, y))) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (debugLogs && debugPrintCandidates)
            Debug.Log($"[PlaceRandomTiles:{debugTag}] tileValue={tileValue} countWanted={count} candidates={candidates.Count}", this);

        if (candidates.Count == 0) return;

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
            if (grid[p.x, p.y] != TILE_EMPTY) continue;
            grid[p.x, p.y] = tileValue;
            placed++;
        }

        if (debugLogs && debugPrintCandidates)
            Debug.Log($"[PlaceRandomTiles:{debugTag}] tileValue={tileValue} placed={placed}/{count}", this);
    }

    // ---------------- DEBUG helpers ----------------

    private string PrefabName(GameObject go) => go == null ? "NULL" : go.name;

    private string DumpPrefabArray(GameObject[] arr)
    {
        if (arr == null) return "(null)";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"(len={arr.Length})");
        for (int i = 0; i < arr.Length; i++)
            sb.AppendLine($"  [{i}] = {(arr[i] == null ? "NULL" : arr[i].name)}");
        return sb.ToString();
    }

    private int CountTiles(int[,] grid, int value, int w, int h)
    {
        int c = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (grid[x, y] == value) c++;
        return c;
    }

    private string ListPositions(int[,] grid, int value, int w, int h)
    {
        StringBuilder sb = new StringBuilder();
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (grid[x, y] == value)
                    sb.AppendLine($"  ({x},{y})");
        return sb.ToString();
    }
}