using UnityEngine;
using SaintsField;
using SaintsField.Playa;
using System.Collections.Generic;

[System.Serializable]
public class MovementStatistics
{
    [SerializeField] public float speed;
    [SerializeField] public float acceleration;
    [SerializeField] public float decceleration;
}
[System.Serializable]
public class JumpStatistics
{
    [LayoutStart("Basic", ELayout.FoldoutBox)]
    [SerializeField] public int jumpAmount;
    [ReadOnly][SerializeField] public int currentJumpAmount;
    [SerializeField] public float jumpForce;
    [LayoutStart("Checks", ELayout.FoldoutBox)]
    [SerializeField][ReadOnly] public bool isGrounded;
    [SerializeField] public Vector2 groundCheckLocation;
    [SerializeField] public Vector2 groundCheckSize;
    [LayoutStart("The Feel", ELayout.FoldoutBox)]
    [SerializeField][ReadOnly] public bool canQuickFall;
    [SerializeField] public float quickFallForce;
    [SerializeField] public float quickFallForceForced;
    [SerializeField] public float peakJumpVelocity;
    [LayoutStart("QOL", ELayout.FoldoutBox)]
    [SerializeField] public float coyoteTimeSeconds;
    [SerializeField][ReadOnly] public int coyoteTimeActive; // |-1 = disabled|0 = able to change|1 = active|
    [SerializeField] public float jumpQueueMax;
    [SerializeField] public float jumpQueueClearTimeSeconds;
    [SerializeField][ReadOnly] public int jumpQueue;
}
[System.Serializable]
public class GravityModifications
{
    [SerializeField] public float fallClamp; // clamp the fall speed of the player to make them fall slower
    [SerializeField] public float gravityFallingAccel;
}

public abstract class Movement : MonoBehaviour
{
    [LayoutStart("Basic Movement Statistics", ELayout.FoldoutBox)]
    [SerializeField] public List<MovementStatistics> moveStats;
    [ReadOnly] public MovementStatistics currentMoveStats;
    [System.NonSerialized] public bool currentMoveStatsClaimed;
    [LayoutStart("Inputs", ELayout.FoldoutBox)]
    [SerializeField][ReadOnly] public Vector2 movementInput;
    [LayoutStart("Components", ELayout.FoldoutBox)]
    [LayoutStart("Runtime", ELayout.FoldoutBox)]
    [SerializeField][ReadOnly] public bool moving;
    [SerializeField][ReadOnly] Rigidbody2D rb;

    #region Quick Functions
    protected virtual void ComponentGrab()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void MoveInputUpdate(Vector2 newVal)
    {
        movementInput = newVal;
    }
    #endregion

    #region Templates
    protected virtual void AxisMovement(Vector2 axis)
    {
        // get the input (depending on axis)
        float getInputFromAxis = Vector2.Dot(movementInput, axis) / axis.magnitude;

        float targetSpeed = getInputFromAxis * currentMoveStats.speed;

        float currentSpeed = Vector2.Dot(rb.linearVelocity, axis);
        float speedDif = targetSpeed - currentSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? currentMoveStats.acceleration : currentMoveStats.decceleration;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * axis, ForceMode2D.Force);
    }

    public void Launch(Vector2 direction)
    {
        rb.AddForce(direction, ForceMode2D.Impulse);
    }

    public void Launch(Vector2 direction, float force, bool resetForce)
    {
        if (resetForce)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }
    #endregion
}
