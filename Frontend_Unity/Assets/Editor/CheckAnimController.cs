using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Text;

public class CheckAnimController {
    [MenuItem("Tools/Check Anim Controller")]
    public static void DoCheck() {
        var path = "Assets/Animations/EntrevistadorController.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) {
            Debug.Log("Controller not found at " + path);
            return;
        }
        StringBuilder sb = new StringBuilder();
        foreach (var layer in controller.layers) {
            sb.AppendLine($"Layer: {layer.name}");
            foreach (var state in layer.stateMachine.states) {
                var motion = state.state.motion;
                string motionName = motion != null ? motion.name : "None";
                sb.AppendLine($"  State: {state.state.name} -> Motion: {motionName}");
            }
        }
        Debug.Log(sb.ToString());
    }
}
