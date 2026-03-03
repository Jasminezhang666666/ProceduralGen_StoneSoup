using System.Collections;
using UnityEngine;

// Dragon AI (2D):
// - Moves like the basic enemy
// - Idles between steps
// - Occasionally stops to crouch once, then spawns Egg or Egg_closed
// - Eggs are instantiated at dragon world position (+ offset), NOT Tile.spawnTile
// - Eggs use Collider2D as Trigger (so they won't push the dragon off-center)
// - Lays eggs forever; egg interval can be long and can include 0 (with optional minimum clamp)
public class Dragon : BasicAICreature
{
    [Header("Rotation Lock (NO ROTATE)")]
    public bool lockRotation = true;

    private Quaternion _initialRotation;
    private Rigidbody2D _rb2D;

    [Header("Roaming (Idle between steps)")]
    public float timeBetweenMovesMin = 1.2f;
    public float timeBetweenMovesMax = 2.6f;
    private float _nextMoveCounter = 0f;

    [Header("Egg Laying (can be very long; can include 0)")]
    public float timeBetweenEggsMin = 0f;
    public float timeBetweenEggsMax = 12f;

    [Tooltip("Optional: prevent instant back-to-back eggs when random hits 0. Set to 0 to allow machine-gun eggs.")]
    public float minEggCooldown = 0.25f;

    [Tooltip("Extra random added ONLY to the first egg to desync multiple dragons spawning together.")]
    public float firstEggExtraDesyncMax = 8f;

    private float _nextEggCounter = 0f;

    [Tooltip("50/50 with eggClosedPrefab.")]
    public GameObject eggPrefab;
    public GameObject eggClosedPrefab;

    [Header("Egg Spawn (Instantiate)")]
    [Tooltip("World-space offset added to dragon position when spawning egg.")]
    public Vector3 eggSpawnOffset = Vector3.zero;

    [Tooltip("If true, egg is parented under the Room transform. Otherwise under dragon's parent.")]
    public bool parentEggToRoom = true;

    [Tooltip("If true, egg uses dragon's rotation; otherwise uses prefab rotation.")]
    public bool eggInheritDragonRotation = false;

    [Header("Animation Params (optional)")]
    public string walkingBoolParam = "Walking";
    public string crouchTriggerParam = "Crouch";
    public string crouchStateName = "";
    public float crouchAnimDuration = 0.8f;

    private bool _layingEgg = false;

    public override void Start()
    {
        base.Start();

        _initialRotation = transform.rotation;
        _rb2D = GetComponent<Rigidbody2D>();

        ApplyRotationLock2D();

        // Move timer starts randomized
        _nextMoveCounter = Random.Range(timeBetweenMovesMin, timeBetweenMovesMax);

        // Egg timer: randomized + extra desync to avoid simultaneous laying
        float baseEgg = Random.Range(timeBetweenEggsMin, timeBetweenEggsMax);
        float desync = Random.Range(0f, Mathf.Max(0f, firstEggExtraDesyncMax));
        _nextEggCounter = ApplyMinEggCooldown(baseEgg + desync);
    }

    private void ApplyRotationLock2D()
    {
        if (!lockRotation) return;

        if (_rb2D != null)
        {
            _rb2D.freezeRotation = true;
            _rb2D.rotation = 0f;
        }

        transform.rotation = _initialRotation;
    }

    private void LateUpdate()
    {
        if (!lockRotation) return;

        if (_rb2D != null) _rb2D.rotation = 0f;
        transform.rotation = _initialRotation;
    }

    private void Update()
    {
        updateSpriteSorting();

        if (_layingEgg) return;

        // Egg timer always ticks.
        if (_nextEggCounter > 0f) _nextEggCounter -= Time.deltaTime;

        // Only count "between moves" while we're actually idling on a tile.
        if (IsAtTarget())
        {
            if (_nextMoveCounter > 0f) _nextMoveCounter -= Time.deltaTime;

            // Egg has priority.
            if (_nextEggCounter <= 0f)
            {
                StartCoroutine(CoLayEgg());
                return;
            }

            // Otherwise move.
            if (_nextMoveCounter <= 0f)
            {
                takeStep();
            }
        }
    }

    private float ApplyMinEggCooldown(float t)
    {
        if (minEggCooldown <= 0f) return t;
        return Mathf.Max(t, minEggCooldown);
    }

    private bool IsAtTarget()
    {
        Vector2 targetGlobalPos = Tile.toWorldCoord(_targetGridPos.x, _targetGridPos.y);
        return Vector2.Distance(transform.position, targetGlobalPos) <= GRID_SNAP_THRESHOLD;
    }

    protected override void takeStep()
    {
        if (_layingEgg) return;

        _neighborPositions.Clear();

        // Test 4-neighborhood, only choose clear tiles.
        Vector2 up = new Vector2(_targetGridPos.x, _targetGridPos.y + 1);
        if (pathIsClear(toWorldCoord(up))) _neighborPositions.Add(up);

        Vector2 right = new Vector2(_targetGridPos.x + 1, _targetGridPos.y);
        if (pathIsClear(toWorldCoord(right))) _neighborPositions.Add(right);

        Vector2 down = new Vector2(_targetGridPos.x, _targetGridPos.y - 1);
        if (pathIsClear(toWorldCoord(down))) _neighborPositions.Add(down);

        Vector2 left = new Vector2(_targetGridPos.x - 1, _targetGridPos.y);
        if (pathIsClear(toWorldCoord(left))) _neighborPositions.Add(left);

        if (_neighborPositions.Count > 0)
        {
            _targetGridPos = GlobalFuncs.randElem(_neighborPositions);
        }

        // Reset idle timer for NEXT time we arrive and idle.
        _nextMoveCounter = Random.Range(timeBetweenMovesMin, timeBetweenMovesMax);
    }

    private IEnumerator CoLayEgg()
    {
        // Wait until we fully arrive.
        while (!IsAtTarget())
            yield return null;

        _layingEgg = true;

        // Hard stop.
        moveViaVelocity(Vector2.zero, 0f, moveAcceleration);
        if (_anim != null && !string.IsNullOrEmpty(walkingBoolParam))
            _anim.SetBool(walkingBoolParam, false);

        // Play crouch once.
        if (_anim != null)
        {
            if (!string.IsNullOrEmpty(crouchStateName))
                _anim.Play(crouchStateName, 0, 0f);
            else if (!string.IsNullOrEmpty(crouchTriggerParam))
                _anim.SetTrigger(crouchTriggerParam);
        }

        yield return new WaitForSeconds(crouchAnimDuration);

        SpawnEggAtDragonWorldPos_2DTriggerFriendly();

        // Reset egg timer (can be long; can include 0).
        _nextEggCounter = ApplyMinEggCooldown(Random.Range(timeBetweenEggsMin, timeBetweenEggsMax));

        // Reset move idle timer so we don't instantly step after laying.
        _nextMoveCounter = Random.Range(timeBetweenMovesMin, timeBetweenMovesMax);

        // IMPORTANT: do NOT call takeStep() here. Let Update decide after idling.
        _layingEgg = false;
    }

    private void SpawnEggAtDragonWorldPos_2DTriggerFriendly()
    {
        GameObject chosen = null;

        if (eggPrefab != null && eggClosedPrefab != null)
            chosen = (Random.value < 0.5f) ? eggPrefab : eggClosedPrefab;
        else
            chosen = (eggPrefab != null) ? eggPrefab : eggClosedPrefab;

        if (chosen == null) return;

        // Parent: prefer Room for clean hierarchy.
        Transform parentTf = null;
        if (parentEggToRoom)
        {
            Room room = GetComponentInParent<Room>();
            parentTf = (room != null) ? room.transform : null;
        }
        if (parentTf == null) parentTf = (transform.parent != null) ? transform.parent : transform;

        Vector3 spawnPos = transform.position + eggSpawnOffset;
        Quaternion spawnRot = eggInheritDragonRotation ? transform.rotation : chosen.transform.rotation;

        GameObject eggObj = Instantiate(chosen, spawnPos, spawnRot, parentTf);

        // Eggs are Trigger now, so they shouldn't push the dragon.
        // Still, if you ever switch them back to non-trigger, this keeps it safe:
        var eggCol = eggObj.GetComponentInChildren<Collider2D>();
        var dragonCol = GetComponentInChildren<Collider2D>();
        if (eggCol != null && dragonCol != null)
        {
            Physics2D.IgnoreCollision(eggCol, dragonCol, true);
        }
    }
}