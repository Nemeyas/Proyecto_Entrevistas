using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class FixGmanRig2
{
    [MenuItem("Tools/Fix Gman Rig 2")]
    public static void Fix()
    {
        var path = "Assets/Models/scene.fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null) {
            // Force reset mapping
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            // Now read the generated description
            var hd = importer.humanDescription;
            var newBones = new List<HumanBone>();
            foreach(var b in hd.human) {
                if (!b.humanName.Contains("Finger") && !b.humanName.Contains("Thumb") && !b.humanName.Contains("Eye")) {
                    newBones.Add(b);
                }
            }
            
            hd.human = newBones.ToArray();
            importer.humanDescription = hd;
            importer.SaveAndReimport();
            Debug.Log("Reimported with fingers and eyes removed, reset from base model.");
        } else {
            Debug.LogError("Importer not found.");
        }
    }
}
