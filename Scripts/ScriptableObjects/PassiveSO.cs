using UnityEngine;
using SaintsField;
using SaintsField.Playa;
using System.Collections.Generic;

[System.Serializable]
public class PassiveEffect
{
    public enum PassiveType
    {
        statModifier = 0,
    }
    public enum ModType
    {
        add,
        subtract,
        divide,
        multiply
    }

    [SerializeField] public PassiveType type;

    // basic stat modification
    [ShowIf(nameof(type), PassiveType.statModifier)][SerializeField] public StatisticsSO.StatCategory category;
    [ShowIf(nameof(type), PassiveType.statModifier)]
    [MenuDropdown(nameof(GetStatOptions))][SerializeField] public string statName;
    [ShowIf(nameof(type), PassiveType.statModifier)][SerializeField] public ModType modType;
    [ShowIf(nameof(type), PassiveType.statModifier)][SerializeField] public float value;

    // Populates the statName dropdown from whichever category is currently selected above - add a
    // float field to any *Statistics class and it shows up here automatically, no code changes.
    DropdownList<string> GetStatOptions()
    {
        DropdownList<string> options = new();
        foreach (string name in StatFieldUtility.GetFieldNames(StatisticsSO.GetCategoryType(category)))
            options.Add(name, name);
        return options;
    }

    /// <summary>Applies this effect to the given (runtime) StatisticsSO in place.</summary>
    public void Apply(StatisticsSO stats)
    {
        switch (type)
        {
            case PassiveType.statModifier:
                ApplyStatModifier(stats);
                break;
        }
    }

    void ApplyStatModifier(StatisticsSO stats)
    {
        object statObject = stats.GetCategoryObject(category);
        float newValue = StatFieldUtility.GetValue(statObject, statName);

        switch (modType)
        {
            case ModType.add:
                newValue += value;
                break;
            case ModType.subtract:
                newValue -= value;
                break;
            case ModType.divide:
                newValue /= value;
                break;
            case ModType.multiply:
                newValue *= value;
                break;
        }

        StatFieldUtility.SetValue(statObject, statName, newValue);
    }
}

[CreateAssetMenu(fileName = "PassiveItem", menuName = "ScriptableObjects/PassiveItem")]
public class PassiveSO : ItemSO
{
    [LayoutStart("Passive Specific", ELayout.FoldoutBox)]
    [SerializeField] List<PassiveEffect> effects;

    /// <summary>
    /// Applies this passive's stat modifications directly to the consumer's runtimeStats - the
    /// runtime-only StatisticsSO instance. playerStats (the character's base sheet) is never
    /// touched, so runtimeStats stays a disposable, re-derivable tracker for the current run.
    /// </summary>
    public void ApplyPassive(Entity entity)
    {
        foreach (PassiveEffect effect in effects)
            effect.Apply(entity.runtimeStats);
    }
}
