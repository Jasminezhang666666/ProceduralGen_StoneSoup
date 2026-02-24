using UnityEngine;

public class MoveWall_tile : Wall
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.0f;

    // 0 = Up, 1 = Right, 2 = Down, 3 = Left
    private int _dirIndex;
    private Vector2 _dir;
    private Vector2 _lastPos;
    private bool _initialized;

    public override void init()
    {
        base.init();
        EnsureBody();
        InitDirectionIfNeeded();
    }

    private void EnsureBody()
    {   
        _body = gameObject.AddComponent<Rigidbody2D>();
        _body.gravityScale = 0f;
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void InitDirectionIfNeeded()
    {
        if (_initialized) return;

        _dirIndex = Random.Range(0, 4); // random start direction
        ApplyDirIndex();
        _lastPos = _body.position;

        _initialized = true;
    }

    private void FixedUpdate()
    {
        _lastPos = _body.position;

        Vector2 next = _body.position + _dir * (moveSpeed * Time.fixedDeltaTime);
        _body.MovePosition(next);

        updateSpriteSorting(); // keep correct draw order while moving
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.isTrigger) return;

        // snap back so we don't end up inside colliders
        _body.MovePosition(_lastPos);

        TurnClockwise();
    }

    private void TurnClockwise()
    {
        _dirIndex = (_dirIndex + 1) % 4;
        ApplyDirIndex();
    }

    private void ApplyDirIndex()
    {
        switch (_dirIndex)
        {
            case 0: _dir = Vector2.up; break;
            case 1: _dir = Vector2.right; break;
            case 2: _dir = Vector2.down; break;
            default: _dir = Vector2.left; break;
        }
    }
}