using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class RevertController {
    [MenuItem("Tools/Revert Controller")]
    public static void DoRevert() {
        var pasivo = GameObject.Find("ModeloPasivo");
        if (pasivo != null) {
            var anim = pasivo.GetComponent<Animator>();
            if (anim != null) {
                var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/EntrevistadorController.controller");
                anim.runtimeAnimatorController = baseController;
                Debug.Log("Reverted ModeloPasivo to EntrevistadorController");
            }
        }
    }
}
