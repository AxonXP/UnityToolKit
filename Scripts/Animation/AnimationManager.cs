using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives an Animator purely from inspector-configured bindings to other components' fields/
/// properties (see AnimatorBinding) - no other script needs a reference to this component or
/// needs to know it exists, so it stays fully optional/portable per-project.
/// </summary>
public class AnimationManager : MonoBehaviour
{
    [SerializeField] public Animator animator;
    [SerializeField] List<AnimatorBinding> bindings = new();

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        foreach (AnimatorBinding binding in bindings)
            binding.Initialize(animator);
    }

    void Update()
    {
        foreach (AnimatorBinding binding in bindings)
            binding.Apply(animator);
    }
}
