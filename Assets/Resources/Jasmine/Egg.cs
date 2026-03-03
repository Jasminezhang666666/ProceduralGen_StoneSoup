using UnityEngine;

public class Egg : Tile
{
    [Header("Explosion")]
    public float explosionRadius = 2f;
    public float explosionForce = 2000f;
    public int explosionDamage = 2;

    [Header("Explosion SFX")]
    [Tooltip("Played when the egg explodes (before it destroys itself).")]
    public AudioClip explosionSFX;

    [Header("Trigger")]
    public bool explodeOnPlayerTouch = true;
    public bool explodeOnAnyTileTouch = false;

    [Header("Safety")]
    public bool oneShot = true;
    private bool _exploded = false;

    public override void init()
    {
        base.init();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_alive) return;
        if (oneShot && _exploded) return;

        Tile otherTile = other.GetComponent<Tile>();
        if (otherTile == null) return;

        if (explodeOnAnyTileTouch)
        {
            ExplodeAndDie();
            return;
        }

        if (explodeOnPlayerTouch && otherTile.hasTag(TileTags.Player))
        {
            ExplodeAndDie();
        }
    }

    private void ExplodeAndDie()
    {
        if (oneShot && _exploded) return;
        _exploded = true;

        // ✅ Play explosion sound (your AudioManager only supports 1-arg overload)
        if (explosionSFX != null)
        {
            AudioManager.playAudio(explosionSFX);
        }

        Collider2D[] maybeColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D maybeCollider in maybeColliders)
        {
            Tile t = maybeCollider.GetComponent<Tile>();
            if (t == null) continue;
            if (t == this) continue;

            t.takeDamage(this, explosionDamage, DamageType.Explosive);

            Vector2 dir = (t.transform.position - transform.position);
            t.addForce(dir * explosionForce);
        }

        takeDamage(this, 9999, DamageType.Explosive);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}