using SaintsField.Playa;
using UnityEditor.Search;
using UnityEngine;

public class OverheadMovement : Movement
{
    public enum MovementState
    {
        basic,
        inactive,
    }

    [LayoutStart("Overhead Movement", ELayout.FoldoutBox)]
    [SerializeField] MovementState moveState;

    void Start()
    {
        // may already be set by Entity.RecalculateRuntimeStats (runs in Awake, before this) -
        // only fall back to the designer-authored preset if nothing claimed it first
        if (!currentMoveStatsClaimed)
            currentMoveStats = moveStats[0];
        ComponentGrab();
    }

    void FixedUpdate()
    {
        switch (moveState)
        {
            case MovementState.basic:
                AxisMovement(Vector2.right);
                AxisMovement(Vector2.up);
                if (movementInput == Vector2.zero)
                    moving = false;
                else
                    moving = true;
                break;
            default:
                moving = false;
                break;
        }
    }
}
