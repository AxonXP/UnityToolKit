using SaintsField;
using SaintsField.Playa;
using UnityEngine;

public class SameSceneDoor : MonoBehaviour
{
    [LayoutStart("Door Settings", ELayout.FoldoutBox)]
    [SerializeField] public bool open;
    [SerializeField] Vector3 exitPosition;
    [SerializeField] Vector3 enterDirection;

    void OnTriggerStay2D(Collider2D collision)
    {
        if (open
            && collision.transform.tag == "Player"
            && Vector2.Dot(collision.GetComponentInParent<Movement>().movementInput, ((Vector2)enterDirection).normalized) > 0.3f)
        {
            EnterDoor(collision.gameObject);
        }
    }

    void EnterDoor(GameObject enterer)
    {
        enterer.transform.root.position = exitPosition;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(exitPosition, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.position + enterDirection.normalized);
    }
}