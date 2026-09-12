using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlatformerMovementKeyController : MonoBehaviour
{
    bool holdingAttack = false;
    public UnityEvent pause;
    
    public void OnMove(InputValue value)
    {
        SendMessage("MoveInputUpdate", value.Get<Vector2>());
    }

    // public void OnJump(InputValue value)
    // {
    //     SendMessage("Jump");
    // }

    public void OnAttack(InputValue value)
    {
        holdingAttack = !holdingAttack;
    }

    public void OnPause(InputValue value)
    {
        pause.Invoke();
    }

    public void OnDash(InputValue value)
    {
        SendMessage("Dash");
    }

    void Update()
    {
        if (holdingAttack) BroadcastMessage("SendAttack");
    }
}
