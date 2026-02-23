using System.Collections;
using UnityEngine;

public class Trap : Tile
{
    [SerializeField] private float trapSeconds = 3f;
    private bool _triggered;

    public override void init()
    {
        base.init();
        if (_sprite != null) _sprite.enabled = false; // start invisible
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;

        // robust: collider might be on a child
        Tile otherTile = other.GetComponentInParent<Tile>();
        if (otherTile == null) return;
        if (!otherTile.hasTag(TileTags.Player)) return;

        _triggered = true;

        if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
        if (_sprite != null) _sprite.enabled = true; // reveal trap

        Player player = otherTile.GetComponent<Player>();
        if (player == null) return;

        StartCoroutine(TrapRoutine(player));
    }

    private IEnumerator TrapRoutine(Player player)
    {
        // stop
        if (player.body != null) player.body.linearVelocity = Vector2.zero;

        player.enabled = false; // trap

        yield return new WaitForSeconds(trapSeconds);

        if (player != null) player.enabled = true; // release
        Destroy(gameObject); // remove trap
    }
}