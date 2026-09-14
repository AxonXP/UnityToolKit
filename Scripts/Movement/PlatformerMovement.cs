using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Events;

public class PlatformerMovement : Movement
{
    public enum MovementState
    {
        basic,
        inactive,
    }

    [LayoutStart("Platformer Movement", ELayout.FoldoutBox)]
    [SerializeField] float dashForce;
    [HideInInspector] public UnityEvent dash = new UnityEvent();
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

    public virtual void Dash()
    {
        if (moveState == MovementState.basic)
        {
            dash.Invoke();
            Launch(movementInput.x * Vector2.right, dashForce, true);
        }
    }
}
