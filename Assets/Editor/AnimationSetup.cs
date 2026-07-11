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

        // Load all Aksa Lari sub-sprites
        string spritePath = "Assets/Level 2/Aksa Lari.png";
        var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(spritePath);
        var runSprites = new List<Sprite>();
        foreach (var o in objs)
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

        // 2. Create Idle Clip
        AnimationClip idleClip = new AnimationClip();
        idleClip.frameRate = 12;
        var idleKeyframes = new ObjectReferenceKeyframe[1];
        idleKeyframes[0] = new ObjectReferenceKeyframe { time = 0f, value = runSprites[0] };
        
        var idleBinding = new EditorCurveBinding();
        idleBinding.type = typeof(SpriteRenderer);
        idleBinding.path = "";
        idleBinding.propertyName = "m_Sprite";

        AnimationUtility.SetObjectReferenceCurve(idleClip, idleBinding, idleKeyframes);
        AssetDatabase.CreateAsset(idleClip, folderPath + "/Aksa_Idle.anim");

        // 3. Create Run Clip
        AnimationClip runClip = new AnimationClip();
        runClip.frameRate = 12;
        
        // Loop setting
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

        // 4. Add states to controller
        var rootStateMachine = controller.layers[0].stateMachine;
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        var runState = rootStateMachine.AddState("Run");
        runState.motion = runClip;

        // Transitions
        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toRun.hasExitTime = false;
        toRun.duration = 0f;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully created Animator Controller and Animation Clips at Assets/Assets/Animations!");
    }
}
