using UnityEngine;
using UnityEditor;

public class RestorePanelEntrevista
{
    [MenuItem("Tools/Restore Panel")]
    public static void Restore()
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        GameObject panelEntrevista = null;
        foreach(Transform t in allTransforms)
        {
            if (t.name == "PanelEntrevista" && t.gameObject.scene.isLoaded)
            {
                panelEntrevista = t.gameObject;
                break;
            }
        }

        if (panelEntrevista != null)
        {
            RectTransform rt = panelEntrevista.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                Debug.Log("PanelEntrevista restored to full screen.");
            }
        }
    }
}
