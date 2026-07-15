using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class RebuildAnimator : EditorWindow
{
    [MenuItem("Tools/Rebuild Directional Animator")]
    public static void Rebuild()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Assets/Animations/AksaAnimator_Directional.controller");
        if (controller == null)
        {
            controller = new AnimatorController();
            AssetDatabase.CreateAsset(controller, "Assets/Assets/Animations/AksaAnimator_Directional.controller");
        }

        // Create base layer with state machine
        var layer = new AnimatorControllerLayer();
        layer.name = "Base Layer";
        layer.defaultWeight = 1f;
        layer.stateMachine = new AnimatorStateMachine();
        layer.stateMachine.name = "Base Layer";
        layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        controller.layers = new[] { layer };

        var sm = layer.stateMachine;

        // Parameters
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        // Load clips
        var idleEast = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_east.anim");
        var idleNorthEast = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_north-east.anim");
        var idleNorth = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_north.anim");
        var idleNorthWest = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_north-west.anim");
        var idleWest = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_west.anim");
        var idleSouthWest = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_south-west.anim");
        var idleSouth = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_south.anim");
        var idleSouthEast = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle_south-east.anim");

        var walkEast = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_east.anim");
        var walkNorthEast = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_north-east.anim");
        var walkNorth = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_north.anim");
        var walkNorthWest = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_north-west.anim");
        var walkWest = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_west.anim");
        var walkSouthWest = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_south-west.anim");
        var walkSouth = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_south.anim");
        var walkSouthEast = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Walk_south-east.anim");

        // Idle Blend Tree (2D Freeform Directional)
        var idleBT = new BlendTree();
        idleBT.blendType = BlendTreeType.FreeformDirectional2D;
        idleBT.blendParameter = "MoveX";
        idleBT.blendParameterY = "MoveY";
        idleBT.children = new ChildMotion[] {
            new ChildMotion { motion = idleEast, position = new Vector2(1, 0) },
            new ChildMotion { motion = idleNorthEast, position = new Vector2(1, 1) },
            new ChildMotion { motion = idleNorth, position = new Vector2(0, 1) },
            new ChildMotion { motion = idleNorthWest, position = new Vector2(-1, 1) },
            new ChildMotion { motion = idleWest, position = new Vector2(-1, 0) },
            new ChildMotion { motion = idleSouthWest, position = new Vector2(-1, -1) },
            new ChildMotion { motion = idleSouth, position = new Vector2(0, -1) },
            new ChildMotion { motion = idleSouthEast, position = new Vector2(1, -1) },
        };

        // Walk Blend Tree (2D Freeform Directional)
        var walkBT = new BlendTree();
        walkBT.blendType = BlendTreeType.FreeformDirectional2D;
        walkBT.blendParameter = "MoveX";
        walkBT.blendParameterY = "MoveY";
        walkBT.children = new ChildMotion[] {
            new ChildMotion { motion = walkEast, position = new Vector2(1, 0) },
            new ChildMotion { motion = walkNorthEast, position = new Vector2(1, 1) },
            new ChildMotion { motion = walkNorth, position = new Vector2(0, 1) },
            new ChildMotion { motion = walkNorthWest, position = new Vector2(-1, 1) },
            new ChildMotion { motion = walkWest, position = new Vector2(-1, 0) },
            new ChildMotion { motion = walkSouthWest, position = new Vector2(-1, -1) },
            new ChildMotion { motion = walkSouth, position = new Vector2(0, -1) },
            new ChildMotion { motion = walkSouthEast, position = new Vector2(1, -1) },
        };

        // States
        var idleState = sm.AddState("Idle");
        idleState.motion = idleBT;
        idleState.writeDefaultValues = true;

        var walkState = sm.AddState("Walk");
        walkState.motion = walkBT;
        walkState.writeDefaultValues = true;

        // Transitions
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.1f;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.1f;

        sm.defaultState = idleState;

        AssetDatabase.SaveAssets();
        Debug.Log("Directional Animator Controller rebuilt successfully!");
    }
}