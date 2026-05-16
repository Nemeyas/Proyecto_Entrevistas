using UnityEngine;
using UnityEditor;

public class FixAv
{
    [MenuItem("Tools/FixAv")]
    static void Do()
    {
        string p = "Assets/Models/entrevistador pasivo/Sitting Talking (1).fbx";
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        GameObject go = null;
        foreach (var t in all)
        {
            if (t.name == "ModeloPasivo" && t.gameObject.scene.IsValid())
            {
                go = t.gameObject;
                break;
            }
        }
        if (go == null) { Debug.LogError("ModeloPasivo not found"); return; }

        var anim = go.GetComponent<Animator>();
        if (anim == null) { Debug.LogError("No Animator"); return; }

        Object[] subs = AssetDatabase.LoadAllAssetsAtPath(p);
        Avatar av = null;
        foreach (var s in subs)
        {
            if (s is Avatar) { av = (Avatar)s; break; }
        }
        if (av == null) { Debug.LogError("No Avatar in FBX. Sub-assets: " + subs.Length); return; }

        anim.avatar = av;
        EditorUtility.SetDirty(go);
        Debug.Log("Avatar assigned: " + av.name + " isHuman=" + av.isHuman);
    }
}
