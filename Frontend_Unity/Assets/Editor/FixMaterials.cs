using UnityEngine;
using UnityEditor;

public class FixMaterials
{
    [MenuItem("Tools/Fix Gman Materials")]
    public static void Fix()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Models" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                // For Standard Shader
                if (mat.HasProperty("_Mode"))
                {
                    mat.SetFloat("_Mode", 0); // Opaque
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = -1;
                }
                
                // For URP (Universal Render Pipeline) Lit/Simple Lit
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 0); // 0 = Opaque
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.renderQueue = -1;
                }
                
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Materials fixed to Opaque.");
    }
}
