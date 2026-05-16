using UnityEngine;
using UnityEditor;

public class ApplyAvatar {
    [MenuItem("Tools/Apply Avatar")]
    public static void DoApply() {
        var pasivo = GameObject.Find("ModeloPasivo");
        if (pasivo != null) {
            var anim = pasivo.GetComponent<Animator>();
            if (anim != null) {
                // Find the avatar in the FBX
                string path = "Assets/Models/entrevistador pasivo/Sitting Talking (1).fbx";
                Avatar avatar = null;
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in assets) {
                    if (a is Avatar) {
                        avatar = a as Avatar;
                        break;
                    }
                }
                
                if (avatar != null) {
                    anim.avatar = avatar;
                    Debug.Log("Assigned Avatar: " + avatar.name);
                } else {
                    Debug.LogWarning("No avatar found in FBX!");
                }
            }
        }
    }
}
