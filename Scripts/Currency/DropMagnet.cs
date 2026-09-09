using UnityEngine;

public class DropMagnet : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent<Pickup>(out Pickup pickup)
            && !pickup.readyToCollect)
        {
            pickup.readyToCollect = true;
            if (pickup is PickupCurrency pickupCurrency)
                CurrencyCollect(pickupCurrency);
        }
    }

    void CurrencyCollect(PickupCurrency pickupCurrency)
    {
        Rigidbody2D rb = pickupCurrency.GetComponent<Rigidbody2D>();
        rb.AddForce((pickupCurrency.transform.position - transform.position) * 10, ForceMode2D.Impulse);
        pickupCurrency.magnetTarget = transform;
    }
}
