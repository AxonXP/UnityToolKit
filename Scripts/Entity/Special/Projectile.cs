using SaintsField;
using SaintsField.Playa;
using UnityEngine;

[System.Serializable]
public class ProjectileStatistics
{
    [SerializeField] public float damage;
    [SerializeField] public float knockback;
    [SerializeField] public float range;
    [SerializeField] public float speed;
    [SerializeField] public float size;
    [SerializeField] public float heightOffset;
}

[System.Serializable]
public class SpecialProjectileStatistics
{
    [SerializeField] public float piercing;
    [SerializeField] public float split;
    [SerializeField] public float chaining;
    [SerializeField] public float explosive;
    [SerializeField] public float ricochet;
    [SerializeField] public float homingStrength;
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Entity))]
public class Projectile : MonoBehaviour
{
    [LayoutStart("Projectile Specific", ELayout.FoldoutBox)]
    [SerializeField] bool rotateTowardVelocity;
    [Tooltip("Add the firing entity's current Rigidbody2D velocity onto this projectile's launch velocity - e.g. shots carry the shooter's movement momentum instead of always leaving at a fixed speed relative to the world.")]
    [SerializeField] bool inheritVelocity;
    [Tooltip("How far chaining/ricochet will look for another entity to redirect toward.")]
    [SerializeField] float seekRange = 5f;
    [Tooltip("Spawned at the hit location when an explosive charge is consumed.")]
    [SerializeField] GameObject explosionPrefab;
    [Tooltip("Stretch/compress the sprite child's Animator playback speed so its current animation's natural length always matches this projectile's lifetime.")]
    [SerializeField] bool syncAnimationToLifetime = true;
    [ShowIf(nameof(syncAnimationToLifetime))]
    [Tooltip("Animator on the sprite child. Auto-found via GetComponentInChildren if left empty.")]
    [SerializeField] Animator spriteAnimator;

    [LayoutStart("Autoset variables", ELayout.FoldoutBox)]
    [ReadOnly][SerializeField] public ProjectileAttack firedBy;
    [ReadOnly][SerializeField] float lifetime = 99; //debug in case start projectile is called late and update calls before it

    [LayoutStart("Autoset attributes", ELayout.FoldoutBox)]
    [ReadOnly][SerializeField] public ProjectileStatistics statistics;
    [ReadOnly][SerializeField] public SpecialProjectileStatistics specialStatistics;

    public void SetUpProjectile(ProjectileStatistics projectileStats, SpecialProjectileStatistics specialProjectileStats)
    {
        statistics = projectileStats;
        specialStatistics = specialProjectileStats;

        HitBox hitBox = GetComponentInChildren<HitBox>();
        hitBox.damage = statistics.damage;
        hitBox.knockBackPower = statistics.knockback;

        transform.localScale = statistics.size * Vector3.one;

        if (TryGetComponent(out OverheadFakeVertical overheadFakeVertical))
            overheadFakeVertical.SetHeight(statistics.heightOffset);
    }

    public void StartProjectile(Vector2 direction)
    {
        lifetime = statistics.range / statistics.speed;
        Invoke(nameof(ReachedRange), lifetime);

        if (syncAnimationToLifetime)
            SyncAnimationSpeed();

        Direct(direction);

        if (inheritVelocity && firedBy != null && firedBy.TryGetComponent(out Rigidbody2D shooterRb))
            GetComponent<Rigidbody2D>().linearVelocity += shooterRb.linearVelocity;
    }

    // Reads the sprite child's current animation state length at the default speed of 1, then sets
    // Animator.speed so that natural length stretches/compresses to exactly this projectile's
    // lifetime - so the animation finishes right as the projectile despawns, regardless of range/speed.
    void SyncAnimationSpeed()
    {
        if (spriteAnimator == null)
            spriteAnimator = GetComponentInChildren<Animator>();
        if (spriteAnimator == null || lifetime <= 0)
            return;

        float clipLength = spriteAnimator.GetCurrentAnimatorStateInfo(0).length;
        if (clipLength > 0)
            spriteAnimator.speed = clipLength / lifetime;
    }

    public void Direct(Vector2 direction)
    {
        GetComponent<Rigidbody2D>().linearVelocity = direction * statistics.speed;
        if (rotateTowardVelocity)
            RotateTowardVelocity();
    }

    protected virtual void RotateTowardVelocity()
    {
        transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(GetComponent<Rigidbody2D>().linearVelocity.y, GetComponent<Rigidbody2D>().linearVelocity.x) * Mathf.Rad2Deg);
    }

    void ReachedRange()
    {
        CheckSplit();
        CheckExplode();
        GetComponent<Entity>().Death();
    }

    #region Proj Callbacks
    public void ProjectileContact(Collider2D other) // touched something (check collider)
    {
        if (other.GetComponentInParent<Entity>())
        {
            CheckSplit();
            CheckExplode();

            if (CheckChain(other)) return;
            if (CheckRichocet(other)) return;
            if (CheckPierce()) return;
            GetComponent<Entity>().Death();
        }
        else
        {
            CheckSplit();
            CheckExplode();

            if (CheckRichocet(other)) return;
            GetComponent<Entity>().Death();
        }
    }
    #endregion

    #region Helpers
    bool CheckPierce()
    {
        if (specialStatistics.piercing > 0)
        {
            specialStatistics.piercing -= 1;
            return true;
        }
        else
            return false;
    }
    bool CheckChain(Collider2D justHit)
    {
        if (specialStatistics.chaining <= 0)
            return false;

        // no other entity in range - act as if chaining didn't happen at all: don't consume the
        // charge, don't redirect, just fall through so ProjectileContact tries ricochet next

        statistics.speed *= 1.5f;
        lifetime += 1f;

        Vector2 direction = FindNearest(justHit);
        if (direction == Vector2.zero)
            return false;

        specialStatistics.chaining -= 1;
        Direct(direction);
        return true;
    }
    // Bounces off whatever it touched - reflects the current travel direction across the collider's
    // actual surface normal at the point of impact. A per-axis sign-mirror can't be made consistent
    // here: whenever the contact point shares an X or Y coordinate with the projectile (very common
    // against a straight wall), that delta component is ~0, and Mathf.Sign(0) is 1 in Unity - so
    // the "flip if signs match" check degenerates into "flip only if velocity happens to be
    // positive," which has nothing to do with the actual approach angle. A true reflection has no
    // such branch to go wrong, so it's consistent at every angle.
    bool CheckRichocet(Collider2D other)
    {
        if (specialStatistics.ricochet > 0)
        {
            specialStatistics.ricochet -= 1;

            Vector2 currentDirection = GetComponent<Rigidbody2D>().linearVelocity.normalized;
            Vector2 normal = FindSurfaceNormal(other, currentDirection);

            Direct(Vector2.Reflect(currentDirection, normal));

            return true;
        }
        else
            return false;
    }
    // Casts a short ray back along the incoming direction - far enough to guarantee starting
    // outside the collider the projectile just entered - so the ray crosses the real surface and
    // RaycastHit2D.normal gives the exact normal at the point of impact, rather than the
    // ClosestPoint approximation (which is only exact for convex shapes and can be a little off
    // near corners or concave colliders). Falls back to ClosestPoint if the ray somehow misses -
    // e.g. a collider small enough that the cast distance overshoots past it.
    Vector2 FindSurfaceNormal(Collider2D other, Vector2 direction)
    {
        float castDistance = statistics.speed * Time.deltaTime + 0.1f;
        Vector2 castOrigin = (Vector2)transform.position - direction * castDistance;

        RaycastHit2D hit = Physics2D.Raycast(castOrigin, direction, castDistance * 2f, 1 << other.gameObject.layer);

        if (hit.collider == other)
            return hit.normal;

        return ((Vector2)transform.position - other.ClosestPoint(transform.position)).normalized;
    }
    // Spawns 2 fragments perpendicular (left/right) to the original travel direction. The fragments
    // don't need any special "ignore this target" logic themselves - HurtBox already makes the hit
    // entity briefly invincible before this even runs, so the fragments spawning right on top of it
    // just won't register another hit during that window.
    bool CheckSplit()
    {
        if (specialStatistics.split > 0)
        {
            specialStatistics.split -= 1;

            Vector2 direction = GetComponent<Rigidbody2D>().linearVelocity.normalized;
            Vector2 left = new Vector2(-direction.y, direction.x);
            Vector2 right = new Vector2(direction.y, -direction.x);

            SpawnSplitFragment(left);
            SpawnSplitFragment(right);

            return true;
        }
        else
            return false;
    }
    // Clones this already-configured projectile instance (same stats, already applied) rather than
    // the temp prefab, so the fragment doesn't need SetUpProjectile called on it again - then zeroes
    // its split charge so fragments don't keep splitting forever.
    void SpawnSplitFragment(Vector2 direction)
    {
        GameObject fragment = Instantiate(gameObject, transform.position, transform.rotation);
        Projectile fragmentProjectile = fragment.GetComponent<Projectile>();
        fragmentProjectile.StartProjectile(direction);
    }
    bool CheckExplode()
    {
        if (specialStatistics.explosive > 0)
        {
            specialStatistics.explosive -= 1;

            if (explosionPrefab != null)
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            return true;
        }
        else
            return false;
    }
    // Closest enemy (an Entity that also has EnemyScript - so chaining can't redirect into the
    // player or some other non-enemy Entity) within seekRange, excluding whoever fired this
    // projectile (so a player's chaining shot doesn't seek back into the player), this projectile
    // itself, and (if given) the entity that was just hit - without that last exclusion, the
    // just-hit entity is almost always the "nearest" one (it's touching the projectile right now),
    // so chaining would just redirect straight back into the same target it already hit instead of
    // jumping to a new one. Returns Vector2.zero when nothing is found, since a normalized
    // direction is never zero - that lets CheckChain tell "no target" apart from a real direction
    // and skip the effect entirely.
    Vector2 FindNearest(Collider2D justHit = null)
    {
        GameObject origin = GetComponentInChildren<HitBox>().originObject;
        GameObject justHitEntity = justHit != null ? justHit.transform.root.gameObject : null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, seekRange);

        Entity nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!hit.transform.root.TryGetComponent(out Entity candidate))
                continue;
            if (!hit.transform.root.TryGetComponent<EnemyScript>(out _))
                continue;
            if (candidate.gameObject == origin || candidate.gameObject == gameObject || candidate.gameObject == justHitEntity)
                continue;

            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        if (nearest == null)
            return Vector2.zero;

        return ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
    }
    #endregion
}
