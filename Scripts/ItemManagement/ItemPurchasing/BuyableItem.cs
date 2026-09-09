using System.Collections.Generic;
using SaintsField.Playa;
using UnityEngine;

public class BuyableItem : MonoBehaviour
{
    [LayoutStart("BuyableItem", ELayout.FoldoutBox)]
    [SerializeField] public ItemSO item;
    [Tooltip("Drag in the tooltip objects here (OPTIONAL)")]
    [SerializeField] List<GameObject> toolTips;

    void Start()
    {
        FillToolTips();
        FillIcon();
    }

    // Lets a spawner (e.g. GameManager.CreateShopItems) assign which item this instance sells after
    // Instantiate, without waiting on Start() timing.
    public void SetItem(ItemSO newItem)
    {
        item = newItem;
        FillToolTips();
        FillIcon();
    }

    void FillToolTips()
    {
        if (toolTips == null) return;

        foreach (GameObject toolTipObject in toolTips)
        {
            if (toolTipObject == null) continue;
            if (!toolTipObject.TryGetComponent<ToolTip>(out ToolTip toolTip)) continue;

            toolTip.FillText("Title", item.title);
            toolTip.FillText("Description", item.description);
            toolTip.FillText("Price", item.costs[MoneyManager.Currency.coins].ToString());
        }
    }

    void FillIcon()
    {
        Transform iconTransform = transform.Find("Item Sprite");
        if (iconTransform == null) return;
        if (!iconTransform.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer)) return;

        spriteRenderer.sprite = item.icon;
    }

    public bool AttemptBuy(ItemManager consumer, MoneyManager wallet)
    {
        if (!wallet.CanAfford(item.costs))
            return false;

        wallet.UseMoney(item.costs);
        item.Grant(consumer);
        DeactivateToolTips();
        return true;
    }

    void DeactivateToolTips()
    {
        if (toolTips == null) return;

        foreach (GameObject toolTipObject in toolTips)
        {
            if (toolTipObject == null) continue;

            if (toolTipObject.TryGetComponent<ToolTip>(out ToolTip toolTip))
                toolTip.CloseTip();

            toolTipObject.SetActive(false);
        }
    }
}
