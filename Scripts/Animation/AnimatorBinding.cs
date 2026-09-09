using System;
using System.Collections.Generic;
using System.Reflection;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

/// <summary>
/// One inspector-configured link between a field/property on another component and an Animator
/// parameter. Resolved purely by reflection so the source component never needs to know about
/// AnimationManager - keeps AnimationManager reusable without any other script touching it.
///
/// The parameter's type (Float/Int/Bool/Trigger) always comes from the Animator Controller itself
/// (via ParameterName's AnimatorParam dropdown) rather than a separately-configured enum, so there is
/// only one place that can go stale. Float/Int/Bool parameters poll a matching-typed field/property
/// every frame. Trigger parameters support two different member kinds, since a Trigger is a one-shot
/// pulse rather than a continuous value:
///  - a UnityEvent field: subscribed once, fires the trigger exactly when the event invokes.
///  - a writable bool field/property: polled every frame; the moment it reads true, the trigger
///    fires once and the bool is written back to false - a "consume" flag the source script can set
///    without ever needing to know AnimationManager exists.
/// </summary>
[System.Serializable]
public class AnimatorBinding
{
    [Tooltip("Component (on this GameObject or elsewhere) holding the value/event to read.")]
    [SerializeField] Component source;

    [AnimatorParam]
    [SerializeField] string parameterName;

    [Tooltip("Field/property (Float, Int, Bool) or UnityEvent/writable-bool (Trigger) on Source, filtered to what the selected parameter's type actually accepts.")]
    [MenuDropdown(nameof(GetMemberOptions))]
    [SerializeField] string memberName;

    const BindingFlags ReflectFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Dictionary<Type, (string Name, Type Type, bool Writable)[]> memberInfoCache = new();
    static readonly Dictionary<Type, string[]> eventFieldCache = new();

    // Populates the memberName dropdown based on the AnimatorControllerParameterType of whichever
    // parameter is currently selected above - Trigger shows UnityEvent fields plus writable bools
    // (labelled so it's clear which behavior each option gives), everything else shows fields/
    // properties whose type exactly matches the parameter.
    DropdownList<string> GetMemberOptions()
    {
        DropdownList<string> options = new();
        if (source == null || string.IsNullOrEmpty(parameterName))
            return options;

        AnimatorControllerParameterType? paramType = GetParameterType(GetNearbyAnimator(), parameterName);
        if (paramType == null)
            return options;

        if (paramType == AnimatorControllerParameterType.Trigger)
        {
            foreach (string name in GetEventFieldNames(source.GetType()))
                options.Add($"{name}  (event)", name);
            foreach ((string name, Type memberType, bool writable) in GetAllMembers(source.GetType()))
                if (memberType == typeof(bool) && writable)
                    options.Add($"{name}  (bool, consumed)", name);
        }
        else
        {
            foreach ((string name, Type memberType, bool _) in GetAllMembers(source.GetType()))
                if (MatchesParameterType(memberType, paramType.Value))
                    options.Add(name, name);
        }
        return options;
    }

    // Best-effort Animator lookup for edit-time dropdown filtering only - assumes Source lives on
    // (or near) the same GameObject as the AnimationManager/Animator, which is the intended setup.
    Animator GetNearbyAnimator()
    {
        if (source == null)
            return null;
        return source.GetComponent<Animator>()
            ?? source.GetComponentInParent<Animator>()
            ?? source.GetComponentInChildren<Animator>();
    }

    static (string Name, Type Type, bool Writable)[] GetAllMembers(Type type)
    {
        if (!memberInfoCache.TryGetValue(type, out (string Name, Type Type, bool Writable)[] members))
        {
            List<(string, Type, bool)> found = new();
            foreach (FieldInfo field in type.GetFields(ReflectFlags))
                found.Add((field.Name, field.FieldType, !field.IsInitOnly));
            foreach (PropertyInfo prop in type.GetProperties(ReflectFlags))
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                    found.Add((prop.Name, prop.PropertyType, prop.CanWrite));
            memberInfoCache[type] = members = found.ToArray();
        }
        return members;
    }

    static string[] GetEventFieldNames(Type type)
    {
        if (!eventFieldCache.TryGetValue(type, out string[] names))
        {
            List<string> found = new();
            foreach (FieldInfo field in type.GetFields(ReflectFlags))
                if (field.FieldType == typeof(UnityEvent))
                    found.Add(field.Name);
            eventFieldCache[type] = names = found.ToArray();
        }
        return names;
    }

    static bool MatchesParameterType(Type memberType, AnimatorControllerParameterType paramType) => paramType switch
    {
        AnimatorControllerParameterType.Float => memberType == typeof(float),
        AnimatorControllerParameterType.Int => memberType == typeof(int),
        AnimatorControllerParameterType.Bool => memberType == typeof(bool),
        _ => false,
    };

    // Editor-mode-without-play has no initialized Animator to query (Animator.parameters throws
    // unless playing), so read the Controller asset directly via the editor-only API; Play mode and
    // builds can just ask the live Animator.
    static AnimatorControllerParameterType? GetParameterType(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (animator.runtimeAnimatorController is AnimatorController controller)
                foreach (AnimatorControllerParameter p in controller.parameters)
                    if (p.name == paramName)
                        return p.type;
            return null;
        }
#endif

        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName)
                return p.type;
        return null;
    }

    enum TriggerKind { None, Event, BoolConsume }

    [NonSerialized] MemberInfo member;
    [NonSerialized] int parameterHash;
    [NonSerialized] AnimatorControllerParameterType parameterType;
    [NonSerialized] TriggerKind triggerKind;
    [NonSerialized] bool initialized;

    public void Initialize(Animator animator)
    {
        if (source == null || string.IsNullOrEmpty(memberName) || string.IsNullOrEmpty(parameterName))
        {
            Debug.LogWarning("AnimatorBinding is missing a source, parameter, or member.");
            return;
        }

        parameterHash = Animator.StringToHash(parameterName);
        AnimatorControllerParameterType? paramType = GetParameterType(animator, parameterName);
        if (paramType == null)
        {
            Debug.LogWarning($"AnimatorBinding: parameter '{parameterName}' not found on {(animator == null ? "null" : animator.name)}'s controller.");
            return;
        }
        parameterType = paramType.Value;

        Type type = source.GetType();

        if (parameterType == AnimatorControllerParameterType.Trigger)
        {
            InitializeTrigger(animator, type);
            return;
        }

        member = (MemberInfo)type.GetField(memberName, ReflectFlags) ?? type.GetProperty(memberName, ReflectFlags);
        if (member == null)
        {
            Debug.LogWarning($"AnimatorBinding could not find field/property '{memberName}' on {type.Name}.");
            return;
        }

        if (!MatchesParameterType(MemberType(member), parameterType))
        {
            Debug.LogWarning($"AnimatorBinding: '{memberName}' ({MemberType(member).Name}) on {type.Name} doesn't match parameter '{parameterName}' ({parameterType}).");
            return;
        }

        initialized = true;
    }

    void InitializeTrigger(Animator animator, Type type)
    {
        FieldInfo eventField = type.GetField(memberName, ReflectFlags);
        if (eventField != null && eventField.FieldType == typeof(UnityEvent))
        {
            ((UnityEvent)eventField.GetValue(source)).AddListener(() => animator.SetTrigger(parameterHash));
            triggerKind = TriggerKind.Event;
            initialized = true;
            return;
        }

        MemberInfo candidate = (MemberInfo)type.GetField(memberName, ReflectFlags) ?? type.GetProperty(memberName, ReflectFlags);
        if (candidate != null && MemberType(candidate) == typeof(bool) && IsWritable(candidate))
        {
            member = candidate;
            triggerKind = TriggerKind.BoolConsume;
            initialized = true;
            return;
        }

        Debug.LogWarning($"AnimatorBinding: '{memberName}' on {type.Name} is neither a UnityEvent nor a writable bool - can't drive trigger '{parameterName}'.");
    }

    static Type MemberType(MemberInfo m) => m is FieldInfo field ? field.FieldType : ((PropertyInfo)m).PropertyType;
    static bool IsWritable(MemberInfo m) => m is FieldInfo field ? !field.IsInitOnly : ((PropertyInfo)m).CanWrite;

    object ReadValue() => member is FieldInfo field ? field.GetValue(source) : ((PropertyInfo)member).GetValue(source);
    void WriteValue(object value)
    {
        if (member is FieldInfo field)
            field.SetValue(source, value);
        else
            ((PropertyInfo)member).SetValue(source, value);
    }

    public void Apply(Animator animator)
    {
        if (!initialized)
            return;

        switch (triggerKind)
        {
            case TriggerKind.Event:
                return; // event-driven, nothing to poll
            case TriggerKind.BoolConsume:
                if (Convert.ToBoolean(ReadValue()))
                {
                    animator.SetTrigger(parameterHash);
                    WriteValue(false);
                }
                return;
        }

        object value = ReadValue();
        switch (parameterType)
        {
            case AnimatorControllerParameterType.Float:
                animator.SetFloat(parameterHash, Convert.ToSingle(value));
                break;
            case AnimatorControllerParameterType.Int:
                animator.SetInteger(parameterHash, Convert.ToInt32(value));
                break;
            case AnimatorControllerParameterType.Bool:
                animator.SetBool(parameterHash, Convert.ToBoolean(value));
                break;
        }
    }
}
