using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

public enum Rarity
{
    Common = 0,
    Uncommon = 10,
    Rare = 20,
    Legendary = 30
}

[CreateAssetMenu(fileName = "ItemCollection", menuName = "ScriptableObjects/ItemCollection")]
public class ItemCollectionSO : ScriptableObject
{
    [Tooltip("Every item this collection can offer, paired with its individual rarity tier.")]
    [SaintsDictionary]
    [SerializeField] public SaintsDictionary<ItemSO, Rarity> collection = new();

    [Tooltip("Relative roll weight for each rarity tier, used when picking a random item from the collection.")]
    [SaintsDictionary]
    [SerializeField] public SaintsDictionary<Rarity, float> rarityWeights = new();

    /// <summary>Weighted-random pick across every item in the collection, weighted by each item's
    /// individual rarity tier via rarityWeights - so higher-weight tiers (e.g. Common) come up more
    /// often than lower-weight ones (e.g. Legendary) without needing per-item weights.</summary>
    public ItemSO GetRandomItem()
    {
        float totalWeight = 0f;
        foreach (KeyValuePair<ItemSO, Rarity> entry in collection)
        {
            totalWeight += rarityWeights.GetValueOrDefault(entry.Value);
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        foreach (KeyValuePair<ItemSO, Rarity> entry in collection)
        {
            roll -= rarityWeights.GetValueOrDefault(entry.Value);
            if (roll <= 0f) return entry.Key;
        }

        return null;
    }
}