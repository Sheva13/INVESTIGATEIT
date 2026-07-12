using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

public class AnimationSetup
{
    [MenuItem("Tools/Generate Aksa Animations")]
    public static void SetupPlayerAnimator()
    {
        string folderPath = "Assets/Assets/Animations";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string controllerPath = folderPath + "/AksaAnimator.controller";
        
        // 1. Create Animator Controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("isJumping", AnimatorControllerParameterType.Bool);

        // Load all Aksa Lari sub-sprites (run)
        string runSpritePath = "Assets/Level 2/Aksa Lari.png";
        var runObjs = AssetDatabase.LoadAllAssetRepresentationsAtPath(runSpritePath);
        var runSprites = new List<Sprite>();
        foreach (var o in runObjs)
        {
            if (o is Sprite s && s.name.StartsWith("aksa_run_"))
            {
                runSprites.Add(s);
            }
        }
        runSprites.Sort((a, b) => a.name.CompareTo(b.name));

        if (runSprites.Count == 0)
        {
            Debug.LogError("No Aksa Lari sprites found! Please ensure they are sliced.");
            return;
        }

        // Load all Aksa Lompat sub-sprites (idle + jump)
        string jumpSpritePath = "Assets/Level 2/Aksa Lompat.png";
        var jumpObjs = AssetDatabase.LoadAllAssetRepresentationsAtPath(jumpSpritePath);
        Sprite idleSprite = null;
        var jumpSprites = new List<Sprite>();
        foreach (var o in jumpObjs)
        {
            if (o is Sprite s)
            {
                if (s.name == "aksa_idle_0") idleSprite = s;
                else if (s.name.StartsWith("aksa_jump_") && s.name != "aksa_jump_2") jumpSprites.Add(s);
            }
        }
        jumpSprites.Sort((a, b) => a.name.CompareTo(b.name));

        // 2. Create Idle Clip (using aksa_idle_0 from Lompat)
        AnimationClip idleClip = new AnimationClip();
        idleClip.frameRate = 12;
        var idleKeyframes = new ObjectReferenceKeyframe[1];
        idleKeyframes[0] = new ObjectReferenceKeyframe { time = 0f, value = idleSprite ?? runSprites[0] };
        
        var idleBinding = new EditorCurveBinding();
        idleBinding.type = typeof(SpriteRenderer);
        idleBinding.path = "";
        idleBinding.propertyName = "m_Sprite";

        AnimationUtility.SetObjectReferenceCurve(idleClip, idleBinding, idleKeyframes);
        AssetDatabase.CreateAsset(idleClip, folderPath + "/Aksa_Idle.anim");

        // 3. Create Run Clip
        AnimationClip runClip = new AnimationClip();
        runClip.frameRate = 12;
        
        var settings = AnimationUtility.GetAnimationClipSettings(runClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(runClip, settings);

        var runKeyframes = new ObjectReferenceKeyframe[runSprites.Count + 1];
        for (int i = 0; i < runSprites.Count; i++)
        {
            runKeyframes[i] = new ObjectReferenceKeyframe { time = i * (1f / 12f), value = runSprites[i] };
        }
        runKeyframes[runSprites.Count] = new ObjectReferenceKeyframe { time = runSprites.Count * (1f / 12f), value = runSprites[0] };

        var runBinding = new EditorCurveBinding();
        runBinding.type = typeof(SpriteRenderer);
        runBinding.path = "";
        runBinding.propertyName = "m_Sprite";

        AnimationUtility.SetObjectReferenceCurve(runClip, runBinding, runKeyframes);
        AssetDatabase.CreateAsset(runClip, folderPath + "/Aksa_Run.anim");

        // 4. Create Jump Clip
        AnimationClip jumpClip = new AnimationClip();
        jumpClip.frameRate = 12;
        var jumpSettings = AnimationUtility.GetAnimationClipSettings(jumpClip);
        jumpSettings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(jumpClip, jumpSettings);

        var jumpKeyframes = new ObjectReferenceKeyframe[jumpSprites.Count];
        for (int i = 0; i < jumpSprites.Count; i++)
        {
            jumpKeyframes[i] = new ObjectReferenceKeyframe { time = i * (1f / 12f), value = jumpSprites[i] };
        }

        var jumpBinding = new EditorCurveBinding();
        jumpBinding.type = typeof(SpriteRenderer);
        jumpBinding.path = "";
        jumpBinding.propertyName = "m_Sprite";

        AnimationUtility.SetObjectReferenceCurve(jumpClip, jumpBinding, jumpKeyframes);
        AssetDatabase.CreateAsset(jumpClip, folderPath + "/Aksa_Jump.anim");

        // 5. Add states to controller
        var rootStateMachine = controller.layers[0].stateMachine;
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        var runState = rootStateMachine.AddState("Run");
        runState.motion = runClip;

        var jumpState = rootStateMachine.AddState("Jump");
        jumpState.motion = jumpClip;

        // Transitions: Idle <-> Run (based on Speed)
        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toRun.hasExitTime = false;
        toRun.duration = 0f;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        // AnyState -> Jump (when isJumping == true)
        var toJump = rootStateMachine.AddAnyStateTransition(jumpState);
        toJump.AddCondition(AnimatorConditionMode.If, 0, "isJumping");
        toJump.hasExitTime = false;
        toJump.duration = 0f;

        // Jump -> Idle (when isJumping becomes false, after animation plays)
        var jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isJumping");
        jumpToIdle.hasExitTime = true;
        jumpToIdle.exitTime = 0.9f;
        jumpToIdle.duration = 0f;

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully created Animator Controller and Animation Clips at Assets/Assets/Animations!");
    }
}
