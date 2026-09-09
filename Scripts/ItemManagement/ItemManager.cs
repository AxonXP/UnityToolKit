using SaintsField.Playa;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [LayoutStart("ItemManager", ELayout.FoldoutBox)]
    [SerializeField] List<ItemSO> items = new();

    public virtual void AddItem(ItemSO item)
    {
        items.Add(item);
        GetComponent<Entity>().RecalculateRuntimeStats();
    }

    public void ReapplyPassives(Entity entity)
    {
        foreach (ItemSO item in items)
        {
            if (item is PassiveSO passive)
                passive.ApplyPassive(entity);
        }
    }
}
