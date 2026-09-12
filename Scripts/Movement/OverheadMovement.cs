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
