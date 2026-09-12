using System;
using System.Collections.Generic;
using System.Reflection;
using SaintsField;
using SaintsField.Playa;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("Numeric field/property on Source to read every frame, or a numeric-returning method that takes zero args or one enum arg (e.g. MoneyManager.GetBalance(Currency) - one dropdown entry per enum value).")]
    [MenuDropdown(nameof(GetMemberOptions))]
    [SerializeField] string barMaxValue;

    const BindingFlags ReflectFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Dictionary<Type, (string Label, string Value)[]> memberOptionCache = new();

    // Object/Component/Behaviour/MonoBehaviour methods (GetInstanceID, GetHashCode, ...) are numeric-
    // returning noise every single Source would otherwise show in the dropdown - exclude them.
    static readonly HashSet<Type> excludedDeclaringTypes = new()
    {
        typeof(object), typeof(UnityEngine.Object), typeof(Component), typeof(Behaviour), typeof(MonoBehaviour),
    };

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
    [NonSerialized] MemberInfo maxMember;
    [NonSerialized] MethodInfo maxMethod;
    [NonSerialized] object maxMethodArg;
    [NonSerialized] bool initialized;
    [NonSerialized] List<GameObject> spawnedIcons = new();

    public void Initialize()
    {
        if (source == null || string.IsNullOrEmpty(memberName))
        {
            Debug.LogWarning("GUIElementBinding is missing a source or member.");
            return;
        }

        if (!ResolveMember(source.GetType(), memberName, out member, out method, out methodArg))
            return;

        if (displayMode == DisplayMode.Bar && !string.IsNullOrEmpty(barMaxValue)
            && !ResolveMember(source.GetType(), barMaxValue, out maxMember, out maxMethod, out maxMethodArg))
            return;

        if (!ValidateDisplayRefs())
            return;

        initialized = true;
    }

    // name is either a plain field/property name, or "MethodName|EnumValueName" for a method
    // taking one enum argument (see GetMemberOptionEntries). Used for both memberName and barMaxValue.
    static bool ResolveMember(Type type, string name, out MemberInfo resolvedMember, out MethodInfo resolvedMethod, out object resolvedMethodArg)
    {
        resolvedMember = null;
        resolvedMethod = null;
        resolvedMethodArg = null;

        int sep = name.IndexOf('|');
        string namePart = sep >= 0 ? name.Substring(0, sep) : name;

        if (sep < 0)
        {
            resolvedMember = (MemberInfo)type.GetField(namePart, ReflectFlags) ?? type.GetProperty(namePart, ReflectFlags);
            if (resolvedMember != null)
                return true;
        }

        resolvedMethod = FindMethod(type, namePart, wantsEnumArg: sep >= 0);
        if (resolvedMethod == null)
        {
            Debug.LogWarning($"GUIElementBinding could not find field/property/method '{namePart}' on {type.Name}.");
            return false;
        }

        if (sep >= 0)
        {
            Type enumType = resolvedMethod.GetParameters()[0].ParameterType;
            string enumValueName = name.Substring(sep + 1);
            if (!Enum.IsDefined(enumType, enumValueName))
            {
                Debug.LogWarning($"GUIElementBinding: '{enumValueName}' is not a value of {enumType.Name}.");
                resolvedMethod = null;
                return false;
            }
            resolvedMethodArg = Enum.Parse(enumType, enumValueName);
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

    float ReadValue(MemberInfo readMember, MethodInfo readMethod, object readMethodArg)
    {
        object value = readMethod != null
            ? readMethod.Invoke(source, readMethodArg == null ? null : new[] { readMethodArg })
            : readMember is FieldInfo field ? field.GetValue(source) : ((PropertyInfo)readMember).GetValue(source);
        return Convert.ToSingle(value);
    }

    public void Apply()
    {
        if (!initialized)
            return;

        float value = ReadValue(member, method, methodArg);
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
        float max = maxMember != null || maxMethod != null ? ReadValue(maxMember, maxMethod, maxMethodArg) : 1f;
        barImage.fillAmount = max > 0f ? Mathf.Clamp01(value / max) : 0f;
    }
}
