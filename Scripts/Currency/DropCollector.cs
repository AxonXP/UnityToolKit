using UnityEngine;

/// <summary>Lives on an entity's HurtBox child object. Any Pickup that touches it while
/// ReadyToCollect (i.e. it's already done its knockback-then-magnetize dance, not the initial
/// touch that triggers it) is credited to the parent entity's MoneyManager (found via
/// GetComponentInParent, since this sits one level below the entity root) and consumed.</summary>
public class DropCollector : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent<Pickup>(out Pickup pickup))
        {
            if (pickup.readyToCollect)
            {
                Collect(pickup);
            }
        }
    }

    void Collect(Pickup pickup)
    {
        if (pickup is PickupCurrency pickupCurrency)
        {
            MoneyManager moneyManager = GetComponentInParent<MoneyManager>();
            if (moneyManager != null)
                moneyManager.CollectMoney(pickupCurrency.currencyType, pickupCurrency.amount);
        }
        SendMessageUpwards("PlayParticle", new ParticleInformation("PSPickupCollect", pickup.transform.position));
        Destroy(pickup.gameObject);
    }
}
