using SaintsField;
using SaintsField.Playa;
using UnityEngine;

public class ProjectileHitBox : HitBox
{
    [LayoutStart("Projectile", ELayout.FoldoutBox)]
    [Layer][SerializeField] string wallCollisionLayer;
    // Feedback
    public override void HitSomething(Collider2D target)
    {
        Projectile projectile = GetComponentInParent<Projectile>();
        if (projectile != null)
        {
            projectile.ProjectileContact(target);
        }
        base.HitSomething(target);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(wallCollisionLayer))
        {
            Projectile projectile = GetComponentInParent<Projectile>();
            if (projectile != null)
            {
                projectile.ProjectileContact(collision);
            }
        }
    }
}