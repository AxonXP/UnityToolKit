using SaintsField;
using SaintsField.Playa;
using System.Collections.Generic;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    /// <summary>Add a new currency here to make it spendable/collectible everywhere else -
    /// ItemSO.costs and every method below key off this same enum.</summary>
    public enum Currency
    {
        coins = 0,
        gems = 1
    }

    [LayoutStart("MoneyManager", ELayout.FoldoutBox)]
    [SaintsDictionary]
    [SerializeField] SaintsDictionary<Currency, float> wallets = new();

    [LayoutStart("Drop On Death", ELayout.FoldoutBox)]
    [SerializeField] bool dropOnDeath;
    [Tooltip("Prefab (must have a Pickup component) spawned per currency held in the wallet above when this entity dies.")]
    [ShowIf(nameof(dropOnDeath))]
    [SaintsDictionary]
    [SerializeField] SaintsDictionary<Currency, GameObject> pickupPrefabs = new();
    [Tooltip("Random outward impulse range applied to each dropped pickup so they scatter across the floor instead of landing stacked on top of each other.")]
    [ShowIf(nameof(dropOnDeath))]
    [SerializeField] float dropForceMin = 2f;
    [ShowIf(nameof(dropOnDeath))]
    [SerializeField] float dropForceMax = 5f;

    /// <summary>Matches Health/Entity's SendMessage("Death") convention - if dropOnDeath is set,
    /// spawns one Pickup worth 1 unit per whole unit of currency this wallet is holding (a wallet
    /// of 5.7 coins drops 5 individual coin pickups, not one pickup worth 5.7), each flung in a
    /// random 2D direction so they spread out on the floor.</summary>
    public void Death()
    {
        if (!dropOnDeath) return;

        foreach (KeyValuePair<Currency, float> wallet in wallets)
        {
            if (wallet.Value <= 0f) continue;
            if (!pickupPrefabs.TryGetValue(wallet.Key, out GameObject prefab) || prefab == null) continue;

            int count = Mathf.FloorToInt(wallet.Value);
            for (int i = 0; i < count; i++)
            {
                // create pickup
                GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity);

                // if a pickupt, change the values
                if (instance.TryGetComponent(out PickupCurrency pickup))
                {
                    pickup.currencyType = wallet.Key;
                    pickup.amount = 1f;
                }

                // if has rigidbody, burst it
                if (instance.TryGetComponent(out Rigidbody2D rb))
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    rb.AddForce(direction * Random.Range(dropForceMin, dropForceMax), ForceMode2D.Impulse);
                }
            }
        }
    }

    public float GetBalance(Currency currency)
    {
        return wallets.TryGetValue(currency, out float amount) ? amount : 0f;
    }

    /// <summary>Overwrites a wallet's balance outright (e.g. a spawner rolling a random drop amount)
    /// rather than adding to whatever it already held - see CollectMoney for the additive version.</summary>
    public void SetBalance(Currency currency, float amount)
    {
        wallets[currency] = Mathf.Max(0f, amount);
    }

    public bool HasEnough(Currency currency, float amount)
    {
        return GetBalance(currency) >= amount;
    }

    public bool CanAfford(IDictionary<Currency, float> costs)
    {
        foreach (KeyValuePair<Currency, float> cost in costs)
            if (!HasEnough(cost.Key, cost.Value))
                return false;
        return true;
    }

    public void CollectMoney(Currency currency, float amount)
    {
        if (amount <= 0f) return;
        wallets[currency] = GetBalance(currency) + amount;
    }

    public bool UseMoney(Currency currency, float amount)
    {
        if (!HasEnough(currency, amount)) return false;

        wallets[currency] = GetBalance(currency) - amount;
        return true;
    }

    /// <summary>Spends every currency in costs, but only if all of them are affordable -
    /// never leaves a wallet partially charged.</summary>
    public bool UseMoney(IDictionary<Currency, float> costs)
    {
        if (!CanAfford(costs)) return false;

        foreach (KeyValuePair<Currency, float> cost in costs)
            UseMoney(cost.Key, cost.Value);
        return true;
    }
}
