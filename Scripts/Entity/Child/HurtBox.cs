using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI;
using System.Collections;
using SaintsField;
using SaintsField.Playa;

[RequireComponent(typeof(Collider2D))]
public class HurtBox : MonoBehaviour
{
    [LayoutStart("Modifiers", ELayout.FoldoutBox)]
    [SerializeField][SaintsField.Tag] public string ignoreTag;

    [LayoutStart("Invincibility", ELayout.FoldoutBox)]
    [SerializeField][ReadOnly] protected bool invincible = false;
    [SerializeField] protected float invincibilityTime = 0.1f;

    [LayoutStart("Stats", ELayout.FoldoutBox)]
    [SerializeField][ReadOnly] protected List<GameObject> recentHitBoxes = new List<GameObject>();

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (
            !invincible
            && collision.TryGetComponent<HitBox>(out HitBox hitBox)
            && hitBox.originObject != transform.root.gameObject
            && !recentHitBoxes.Contains(collision.gameObject)
            && ignoreTag != collision.tag
            )
        {
            ProcessHitBoxCollision(hitBox);
        }
    }

    protected virtual void ProcessHitBoxCollision(HitBox hitBox)
    {
        // FIND KNOCKBACK DIRECTION
        Vector2 direction = hitBox.FindGlobalDirection();

        // MAKE GENERAL KNOCKBACK IF NO DIRECTION
        if (direction == Vector2.zero)
            direction = (transform.position - hitBox.transform.position).normalized;

        // PLAY PARTICLES
        transform.root.BroadcastMessage("PlayParticle", new ParticleInformation("PSHitMainDir", Vector2.zero, direction));

        // MAKE INVINCIBLE
        invincible = true;
        StartCoroutine("InvisibilityTime");

        // AFFECT MOVEMENT (PUSH THE PLAYER)
        transform.root.gameObject.BroadcastMessage("Launch", direction * hitBox.knockBackPower);

        // DEAL DAMAGE
        transform.root.gameObject.BroadcastMessage("TakeDamage", hitBox.damage);

        // ADD THE HITBOX TO A RECENT HIT LIST SO THAT IT CANNOT BE DOUBLE HIT
        recentHitBoxes.Add(hitBox.gameObject);

        // SEND FEEDBACK TO - pass our own collider (the thing that was actually touched), not just
        // the root gameobject, so contact-point-dependent effects like ricochet have real geometry
        // to work with instead of guessing from the root's pivot
        hitBox.HitSomething(GetComponent<Collider2D>());
    }

    IEnumerator InvisibilityTime()
    {
        yield return new WaitForSeconds(invincibilityTime);
        invincible = false;
        yield return new WaitForSeconds(invincibilityTime);
        recentHitBoxes.Clear();
    }
}


