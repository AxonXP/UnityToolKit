using System;
using System.Collections.Generic;
using System.Reflection;
using SaintsField;
using SaintsField.Playa;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One inspector-configured HUD element: a Source (what numeric value to read, via reflection - see
/// ElementType) plus a Display (how to present that value - see DisplayMode). Mirrors AnimatorBinding's
/// source+memberName pattern so the source component never needs to know GUIManager exists.
///
/// Both ElementType and DisplayMode follow the project's enum-switched-internally convention (see
/// Weapon-style enums elsewhere): adding a new source kind or a new display kind is one enum case
/// (spaced by 10 so inserting never renumbers saved data) + its [ShowIf]-gated fields + one switch arm
/// in Initialize/Apply - never a new class or a rewrite of existing cases.
/// </summary>
[System.Serializable]
public class GUIElementBinding
{
    public enum ElementType
    {
        NumberStorage = 10,
    }

    public enum DisplayMode
    {
        IconImages = 10,
        Number = 20,
        Bar = 30,
    }

    [SerializeField] ElementType elementType = ElementType.NumberStorage;

    [ShowIf(nameof(elementType), ElementType.NumberStorage)]
    [Tooltip("Component (on this GameObject or elsewhere) holding the numeric value to display.")]
    [SerializeField] Component source;

    [ShowIf(nameof(elementType), ElementType.NumberStorage)]
    [Tooltip("Numeric field/property on Source to read every frame, or a numeric-returning method that takes zero args or one enum arg (e.g. MoneyManager.GetBalance(Currency) - one dropdown entry per enum value).")]
    [MenuDropdown(nameof(GetMemberOptions))]
    [SerializeField] string memberName;

    [SerializeField] DisplayMode displayMode = DisplayMode.Number;

    // Icon Images - e.g. hearts: one instance of iconPrefab per whole point of value, spawned under iconParent.
    [ShowIf(nameof(displayMode), DisplayMode.IconImages)]
    [SerializeField] GameObject iconPrefab;
    [ShowIf(nameof(displayMode), DisplayMode.IconImages)]
    [SerializeField] Transform iconParent;

    // Number - value written straight into a TextMeshPro label.
    [ShowIf(nameof(displayMode), DisplayMode.Number)]
    [SerializeField] TextMeshProUGUI numberText;

    // Bar - value/barMaxValue drives a filled UI Image's fillAmount.
    [ShowIf(nameof(displayMode), DisplayMode.Bar)]
    [SerializeField] Image barImage;
    [ShowIf(nameof(displayMode), DisplayMode.Bar)]
    [SerializeField] float barMaxValue = 1f;

    const BindingFlags ReflectFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Dictionary<Type, (string Label, string Value)[]> memberOptionCache = new();

    // Object/Component/Behaviour/MonoBehaviour methods (GetInstanceID, GetHashCode, ...) are numeric-
    // returning noise every single Source would otherwise show in the dropdown - exclude them.
    static readonly HashSet<Type> excludedDeclaringTypes = new()
    {
        typeof(object), typeof(UnityEngine.Object), typeof(Component), typeof(Behaviour), typeof(MonoBehaviour),
    };

    // Populates the memberName dropdown with numeric fields/properties found on whichever Source is
    // currently assigned above, plus numeric-returning methods that take zero args or a single enum
    // arg (one dropdown entry per enum value, e.g. "GetBalance(coins)") - this is what lets a value
    // that lives behind a keyed lookup (MoneyManager.GetBalance(Currency), an ammo-per-type getter,
    // ...) get bound exactly like a plain field, with no MoneyManager-specific code in here.
    DropdownList<string> GetMemberOptions()
    {
        DropdownList<string> options = new();
        if (source == null)
            return options;

        foreach ((string label, string value) in GetMemberOptionEntries(source.GetType()))
            options.Add(label, value);
        return options;
    }

    static (string Label, string Value)[] GetMemberOptionEntries(Type type)
    {
        if (!memberOptionCache.TryGetValue(type, out (string Label, string Value)[] entries))
        {
            List<(string Label, string Value)> found = new();

            foreach (FieldInfo field in type.GetFields(ReflectFlags))
                if (IsNumeric(field.FieldType))
                    found.Add((field.Name, field.Name));

            foreach (PropertyInfo prop in type.GetProperties(ReflectFlags))
                if (prop.CanRead && prop.GetIndexParameters().Length == 0 && IsNumeric(prop.PropertyType))
                    found.Add((prop.Name, prop.Name));

            foreach (MethodInfo methodInfo in type.GetMethods(ReflectFlags))
            {
                if (methodInfo.IsSpecialName || excludedDeclaringTypes.Contains(methodInfo.DeclaringType) || !IsNumeric(methodInfo.ReturnType))
                    continue;

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 0)
                    found.Add(($"{methodInfo.Name}()", methodInfo.Name));
                else if (parameters.Length == 1 && parameters[0].ParameterType.IsEnum)
                    foreach (string enumName in Enum.GetNames(parameters[0].ParameterType))
                        found.Add(($"{methodInfo.Name}({enumName})", $"{methodInfo.Name}|{enumName}"));
            }

            memberOptionCache[type] = entries = found.ToArray();
        }
        return entries;
    }

    static bool IsNumeric(Type t) => t == typeof(float) || t == typeof(int) || t == typeof(double) || t == typeof(long);

    // Matches how GetMemberOptionEntries built the dropdown: same name, numeric return, and the same
    // 0-arg/1-enum-arg shape - found by scanning rather than Type.GetMethod(name) so an overloaded
    // method name (MoneyManager.UseMoney has two overloads) can never throw AmbiguousMatchException.
    static MethodInfo FindMethod(Type type, string name, bool wantsEnumArg)
    {
        foreach (MethodInfo candidate in type.GetMethods(ReflectFlags))
        {
            if (candidate.Name != name || candidate.IsSpecialName || !IsNumeric(candidate.ReturnType))
                continue;
            ParameterInfo[] parameters = candidate.GetParameters();
            if (wantsEnumArg ? (parameters.Length == 1 && parameters[0].ParameterType.IsEnum) : parameters.Length == 0)
                return candidate;
        }
        return null;
    }

    [NonSerialized] MemberInfo member;
    [NonSerialized] MethodInfo method;
    [NonSerialized] object methodArg;
    [NonSerialized] bool initialized;
    [NonSerialized] List<GameObject> spawnedIcons = new();

    public void Initialize()
    {
        if (source == null || string.IsNullOrEmpty(memberName))
        {
            Debug.LogWarning("GUIElementBinding is missing a source or member.");
            return;
        }

        if (!ResolveMember(source.GetType()))
            return;

        if (!ValidateDisplayRefs())
            return;

        initialized = true;
    }

    // memberName is either a plain field/property name, or "MethodName|EnumValueName" for a method
    // taking one enum argument (see GetMemberOptionEntries).
    bool ResolveMember(Type type)
    {
        int sep = memberName.IndexOf('|');
        string namePart = sep >= 0 ? memberName.Substring(0, sep) : memberName;

        if (sep < 0)
        {
            member = (MemberInfo)type.GetField(namePart, ReflectFlags) ?? type.GetProperty(namePart, ReflectFlags);
            if (member != null)
                return true;
        }

        method = FindMethod(type, namePart, wantsEnumArg: sep >= 0);
        if (method == null)
        {
            Debug.LogWarning($"GUIElementBinding could not find field/property/method '{namePart}' on {type.Name}.");
            return false;
        }

        if (sep >= 0)
        {
            Type enumType = method.GetParameters()[0].ParameterType;
            string enumValueName = memberName.Substring(sep + 1);
            if (!Enum.IsDefined(enumType, enumValueName))
            {
                Debug.LogWarning($"GUIElementBinding: '{enumValueName}' is not a value of {enumType.Name}.");
                method = null;
                return false;
            }
            methodArg = Enum.Parse(enumType, enumValueName);
        }

        return true;
    }

    bool ValidateDisplayRefs()
    {
        switch (displayMode)
        {
            case DisplayMode.IconImages:
                if (iconPrefab == null || iconParent == null)
                {
                    Debug.LogWarning("GUIElementBinding (IconImages) is missing its icon prefab or parent.");
                    return false;
                }
                break;
            case DisplayMode.Number:
                if (numberText == null)
                {
                    Debug.LogWarning("GUIElementBinding (Number) is missing its text element.");
                    return false;
                }
                break;
            case DisplayMode.Bar:
                if (barImage == null)
                {
                    Debug.LogWarning("GUIElementBinding (Bar) is missing its bar image.");
                    return false;
                }
                break;
        }
        return true;
    }

    float ReadValue()
    {
        object value = method != null
            ? method.Invoke(source, methodArg == null ? null : new[] { methodArg })
            : member is FieldInfo field ? field.GetValue(source) : ((PropertyInfo)member).GetValue(source);
        return Convert.ToSingle(value);
    }

    public void Apply()
    {
        if (!initialized)
            return;

        float value = ReadValue();
        switch (displayMode)
        {
            case DisplayMode.IconImages:
                ApplyIcons(value);
                break;
            case DisplayMode.Number:
                ApplyNumber(value);
                break;
            case DisplayMode.Bar:
                ApplyBar(value);
                break;
        }
    }

    void ApplyIcons(float value)
    {
        int count = Mathf.Max(0, Mathf.RoundToInt(value));

        while (spawnedIcons.Count > count)
        {
            GameObject icon = spawnedIcons[0];
            spawnedIcons.RemoveAt(0);
            UnityEngine.Object.Destroy(icon);
        }
        while (spawnedIcons.Count < count)
            spawnedIcons.Add(UnityEngine.Object.Instantiate(iconPrefab, iconParent));
    }

    void ApplyNumber(float value)
    {
        numberText.text = value.ToString("0.##");
    }

    void ApplyBar(float value)
    {
        barImage.fillAmount = barMaxValue > 0f ? Mathf.Clamp01(value / barMaxValue) : 0f;
    }
}
