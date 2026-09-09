using CustomIcons;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

public class ItemSO : ScriptableObject
{
    [LayoutStart("ItemSO", ELayout.FoldoutBox)]
    [ScriptableObjectIcon]
    [AssetPreview(above: true, align: EAlign.Center)]
    [SerializeField] public Sprite icon;
    [SerializeField] public string title;
    [TextArea(3, 10)] public string description;
    [Tooltip("Currencies (and amounts of each) required to buy this item.")]
    [SaintsDictionary]
    [SerializeField] public SaintsDictionary<MoneyManager.Currency, float> costs = new();

    public virtual void Grant(ItemManager consumer)
    {
        consumer.AddItem(this);
    }
}