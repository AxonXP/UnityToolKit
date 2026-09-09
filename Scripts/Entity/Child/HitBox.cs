using SaintsField;
using SaintsField.Playa;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    [LayoutStart("Hitbox", ELayout.FoldoutBox)]
    [SerializeField] public float damage;
    [SerializeField] public float knockBackPower;
    [Tooltip("Direction at angle 0 - keep at 0,0,0 for general outward knock back")]
    [SerializeField] public Vector3 direction;
    [SerializeField][ReadOnly] Vector3 PostAnimDirection;
    [SerializeField] public bool deathOnContact;
    [SerializeField][ReadOnly] public GameObject originObject;

    public Vector3 FindGlobalDirection()
    {
        PostAnimDirection = transform.TransformDirection(direction);
        return PostAnimDirection.normalized;
    }

    // Feedback
    public virtual void HitSomething(Collider2D target)
    {
        if (deathOnContact)
        {
            GetComponentInParent<Entity>().Death();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + FindGlobalDirection());
    }
}
