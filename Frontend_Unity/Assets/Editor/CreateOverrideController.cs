using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class CreateOverrideController {
    [MenuItem("Tools/Create Override Controller")]
    public static void DoCreate() {
        var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/EntrevistadorController.controller");
        if (baseController == null) {
            Debug.LogError("Base controller not found.");
            return;
        }

        var overrideController = new AnimatorOverrideController(baseController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        AnimationClip idleClip = GetClip("Assets/Animations/Sitting Idle.fbx");
        AnimationClip talkingClip = GetClip("Assets/Animations/Sitting Talking.fbx");
        AnimationClip talking1Clip = GetClip("Assets/Models/entrevistador pasivo/Sitting Talking (1).fbx");
        AnimationClip laughClip = GetClip("Assets/Animations/Sitting Laughing.fbx");
        AnimationClip clapClip = GetClip("Assets/Animations/Sitting Clap.fbx");

        if (idleClip == null) Debug.LogWarning("idleClip not found");
        if (talkingClip == null) Debug.LogWarning("talkingClip not found");

        for (int i = 0; i < overrides.Count; i++) {
            var origClip = overrides[i].Key;
            AnimationClip newClip = idleClip; // fallback

            if (origClip.name.Contains("idle")) newClip = idleClip;
            else if (origClip.name.Contains("talking")) newClip = talkingClip;
            else if (origClip.name.Contains("nodding")) newClip = talking1Clip;
            else if (origClip.name.Contains("head_shake")) newClip = idleClip;
            else if (origClip.name.Contains("arguing")) newClip = talkingClip; // fallback to talking
            else if (origClip.name.Contains("stop")) newClip = clapClip;
            else if (origClip.name.Contains("laugh") || origClip.name.Contains("talking_2")) newClip = laughClip;

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(origClip, newClip);
        }

        overrideController.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(overrideController, "Assets/Animations/PasivoOverrideController.overrideController");
        AssetDatabase.SaveAssets();
        Debug.Log("Created PasivoOverrideController with updated clips!");

        var pasivo = GameObject.Find("ModeloPasivo");
        if (pasivo != null) {
            var anim = pasivo.GetComponent<Animator>();
            if (anim != null) {
                anim.runtimeAnimatorController = overrideController;
                Debug.Log("Applied to ModeloPasivo");
            }
        }
    }

    static AnimationClip GetClip(string path) {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets) {
            if (asset is AnimationClip && !asset.name.Contains("__preview__")) {
                return asset as AnimationClip;
            }
        }
        return null;
    }
}
