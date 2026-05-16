using UnityEngine;
using UnityEditor;

public class FixPasivoAvatar
{
    [MenuItem("Tools/Fix Pasivo Avatar")]
    static void Fix()
    {
        string path = "Assets/Models/entrevistador pasivo/Sitting Talking (1).fbx";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("ModelImporter not found for: " + path);
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.SaveAndReimport();
        Debug.Log("FBX avatar set to Humanoid and reimported!");

        // Now assign the avatar to the scene instance
        EditorApplication.delayCall += () =>
        {
            GameObject modeloPasivo = GameObject.Find("ModeloPasivo");
            if (modeloPasivo == null)
            {
                // Search inactive
                var all = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (var t in all)
                {
                    if (t.name == "ModeloPasivo" && t.gameObject.scene.IsValid())
                    {
                        modeloPasivo = t.gameObject;
                        break;
                    }
                }
            }

            if (modeloPasivo != null)
            {
                Animator anim = modeloPasivo.GetComponent<Animator>();
                if (anim != null)
                {
                    // Load the avatar from the reimported FBX
                    Avatar av = AssetDatabase.LoadAssetAtPath<Avatar>(path);
                    if (av == null)
                    {
                        // Try loading sub-assets
                        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                        foreach (var sub in subAssets)
                        {
                            if (sub is Avatar)
                            {
                                av = sub as Avatar;
                                break;
                            }
                        }
                    }

                    if (av != null)
                    {
                        anim.avatar = av;
                        EditorUtility.SetDirty(modeloPasivo);
                        Debug.Log("Avatar assigned to ModeloPasivo: " + av.name);
                    }
                    else
                    {
                        Debug.LogError("No Avatar found in FBX sub-assets");
                    }
                }
            }
            else
            {
                Debug.LogError("ModeloPasivo not found in scene");
            }
        };
    }
}
