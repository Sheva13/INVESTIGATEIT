using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class SetupDirectionalAnimator : EditorWindow
{
    [MenuItem("Tools/Setup Directional Animator")]
    public static void CreateDirectionalAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Assets/Animations/AksaAnimator_Directional.controller");
        if (controller == null)
        {
            controller = new AnimatorController();
            controller.name = "AksaAnimator_Directional";
            AssetDatabase.CreateAsset(controller, "Assets/Assets/Animations/AksaAnimator_Directional.controller");
        }
        
        // Ensure base layer exists
        if (controller.layers.Length == 0)
        {
            var layer = new AnimatorControllerLayer();
            layer.name = "Base Layer";
            layer.defaultWeight = 1f;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.name = "Base Layer";
            controller.layers = new[] { layer };
        }
        
        // Clear existing parameters and add correct ones
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(controller.parameters[i]);
        }
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Ensure state machine exists
        if (rootStateMachine == null)
        {
            rootStateMachine = new AnimatorStateMachine();
            rootStateMachine.name = "Base Layer";
            controller.layers[0].stateMachine = rootStateMachine;
        }
        
        // Clear existing states
        foreach (var state in rootStateMachine.states)
        {
            rootStateMachine.RemoveState(state.state);
        }
        foreach (var trans in rootStateMachine.anyStateTransitions)
        {
            rootStateMachine.RemoveAnyStateTransition(trans);
        }
        
        // Load animation clips
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Idle.anim");
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
        
        var jumpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Assets/Animations/Aksa_Jump.anim");
        
        // Create Idle Blend Tree (2D Freeform Directional)
        var idleBlendTree = new BlendTree();
        idleBlendTree.blendType = BlendTreeType.FreeformDirectional2D;
        idleBlendTree.blendParameter = "MoveX";
        idleBlendTree.blendParameterY = "MoveY";
        
        idleBlendTree.children = new ChildMotion[]
        {
            new ChildMotion { motion = idle, position = new Vector2(0, 0) },
            new ChildMotion { motion = idleEast, position = new Vector2(1, 0) },
            new ChildMotion { motion = idleNorthEast, position = new Vector2(1, 1) },
            new ChildMotion { motion = idleNorth, position = new Vector2(0, 1) },
            new ChildMotion { motion = idleNorthWest, position = new Vector2(-1, 1) },
            new ChildMotion { motion = idleWest, position = new Vector2(-1, 0) },
            new ChildMotion { motion = idleSouthWest, position = new Vector2(-1, -1) },
            new ChildMotion { motion = idleSouth, position = new Vector2(0, -1) },
            new ChildMotion { motion = idleSouthEast, position = new Vector2(1, -1) },
        };
        
        // Create Walk Blend Tree (2D Freeform Directional)
        var walkBlendTree = new BlendTree();
        walkBlendTree.blendType = BlendTreeType.FreeformDirectional2D;
        walkBlendTree.blendParameter = "MoveX";
        walkBlendTree.blendParameterY = "MoveY";
        
        walkBlendTree.children = new ChildMotion[]
        {
            new ChildMotion { motion = walkEast, position = new Vector2(1, 0) },
            new ChildMotion { motion = walkNorthEast, position = new Vector2(1, 1) },
            new ChildMotion { motion = walkNorth, position = new Vector2(0, 1) },
            new ChildMotion { motion = walkNorthWest, position = new Vector2(-1, 1) },
            new ChildMotion { motion = walkWest, position = new Vector2(-1, 0) },
            new ChildMotion { motion = walkSouthWest, position = new Vector2(-1, -1) },
            new ChildMotion { motion = walkSouth, position = new Vector2(0, -1) },
            new ChildMotion { motion = walkSouthEast, position = new Vector2(1, -1) },
        };
        
        // Create states
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleBlendTree;
        
        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = walkBlendTree;
        
        var jumpState = rootStateMachine.AddState("Jump");
        jumpState.motion = jumpClip;
        
        // Transitions
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.1f;
        
        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.1f;
        
        var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0f;
        
        var jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");
        jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0.1f;
        
        var jumpToWalk = jumpState.AddTransition(walkState);
        jumpToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");
        jumpToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        jumpToWalk.hasExitTime = false;
        jumpToWalk.duration = 0.1f;
        
        rootStateMachine.defaultState = idleState;
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Directional Animator Controller created successfully!");
    }
}