using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

namespace SpriteLibraryTools
{
    // Optional post-step after batch clip generation: one Animator state per successfully
    // generated category, reusing an existing controller at the target path if present.
    public static class AnimatorControllerGeneratorService
    {
        public static AnimatorController CreateOrUpdate(string controllerPath, IEnumerable<AnimationGenerationResult> results)
        {
            List<AnimationGenerationResult> usable = results
                .Where(r => r.Clip != null &&
                    (r.Status == AnimationGenerationStatus.Created || r.Status == AnimationGenerationStatus.Updated))
                .ToList();

            if (usable.Count == 0)
                return null;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            var existingStates = new Dictionary<string, AnimatorState>();
            foreach (ChildAnimatorState child in stateMachine.states)
                existingStates[child.state.name] = child.state;

            AnimatorState defaultState = null;
            foreach (AnimationGenerationResult result in usable)
            {
                if (!existingStates.TryGetValue(result.Category, out AnimatorState state))
                    state = stateMachine.AddState(result.Category);

                state.motion = result.Clip;
                defaultState ??= state;
            }

            if (defaultState != null)
                stateMachine.defaultState = defaultState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}
