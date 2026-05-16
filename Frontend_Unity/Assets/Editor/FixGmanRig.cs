using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class FixGmanRig
{
    [MenuItem("Tools/Fix Gman Rig")]
    public static void Fix()
    {
        var path = "Assets/Models/scene.fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null) {
            var hd = importer.humanDescription;
            var newBones = new List<HumanBone>();
            foreach(var b in hd.human) {
                if (!b.humanName.Contains("Finger") && !b.humanName.Contains("Thumb")) {
                    newBones.Add(b);
                }
            }
            // Let's also check if the head is mapped correctly.
            // Some GMan models have bad Neck/Head mappings.
            hd.human = newBones.ToArray();
            importer.humanDescription = hd;
            importer.SaveAndReimport();
            Debug.Log("Reimported with fingers removed from humanoid mapping.");
        } else {
            Debug.LogError("Importer not found.");
        }
    }
}
