using UnityEngine;

public class TouchBuyableItem : BuyableItem
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // The player has multiple colliders sharing one Rigidbody2D (main body + HitBox + HurtBox,
        // all tagged "Player"), so walking into this trigger fires OnTriggerEnter2D once per
        // overlapping collider - respond only to the main body collider (the one directly on the
        // Rigidbody2D's own GameObject) so a purchase attempt doesn't get triggered multiple times.
        if (collision.attachedRigidbody == null || collision.gameObject != collision.attachedRigidbody.gameObject)
            return;

        ItemManager consumer = collision.transform.root.GetComponent<ItemManager>();
        MoneyManager wallet = collision.transform.root.GetComponent<MoneyManager>();
        if (consumer == null || wallet == null) return;

        if (AttemptBuy(consumer, wallet))
        {
            // Destroy first: Destroy() only defers the actual cleanup to end-of-frame, so gameObject
            // and its components stay fully valid for the rest of this call either way - ordering it
            // before the particle SendMessage means a problem playing that effect can't leave an
            // already-paid-for item still sitting in the shop.
            Destroy(gameObject);
            SendMessage("PlayParticle", new ParticleInformation("PSBuyItem"));
        }
        else
        {
            SendMessage("PlayParticle", new ParticleInformation("PSCannotBuyItem"));
        }
    }
}
