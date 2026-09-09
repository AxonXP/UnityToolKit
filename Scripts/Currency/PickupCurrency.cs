using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PickupCurrency : Pickup
{
    public MoneyManager.Currency currencyType;
    public float amount = 1f;
    [Tooltip("How fast velocity ramps up toward maxMagnetSpeed, per second.")]
    public float magnetAcceleration = 40f;
    [Tooltip("Speed cap once magnetized - without one, AddForce-style acceleration never stops " +
        "growing and the pickup overshoots the player, swings back, and oscillates instead of " +
        "smoothly homing in.")]
    public float maxMagnetSpeed = 12f;
    [HideInInspector] public Transform magnetTarget;
    [HideInInspector] public Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (magnetTarget)
        {
            Vector2 direction = ((Vector2)magnetTarget.position - (Vector2)transform.position).normalized;
            Vector2 targetVelocity = direction * maxMagnetSpeed;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, magnetAcceleration * Time.fixedDeltaTime);
        }
    }
}
