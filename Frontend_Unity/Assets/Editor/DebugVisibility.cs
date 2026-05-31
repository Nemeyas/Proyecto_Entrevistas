using UnityEngine;
using UnityEditor;
using System.IO;

public static class DebugVisibility
{
    [MenuItem("Tools/Debug Visibility")]
    public static void DebugNow()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DEBUG VISIBILITY ===");

        var panelMenu = GameObject.Find("PanelMenu");
        if (panelMenu == null)
        {
            sb.AppendLine("PanelMenu not found in scene!");
            File.WriteAllText("Assets/Editor/visibility_log.txt", sb.ToString());
            return;
        }

        sb.AppendLine($"PanelMenu: name={panelMenu.name}, activeInHierarchy={panelMenu.activeInHierarchy}, activeSelf={panelMenu.activeSelf}");
        
        var canvas = panelMenu.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            sb.AppendLine($"Canvas: name={canvas.name}, enabled={canvas.enabled}, renderMode={canvas.renderMode}");
            var cg = canvas.GetComponent<CanvasGroup>();
            if (cg != null) sb.AppendLine($"CanvasGroup on Canvas: alpha={cg.alpha}, interactable={cg.interactable}");
        }
        else
        {
            sb.AppendLine("NO CANVAS FOUND in parent of PanelMenu!");
        }

        var rects = panelMenu.GetComponentsInChildren<RectTransform>(true);
        foreach (var r in rects)
        {
            var go = r.gameObject;
            var canvasGroup = go.GetComponent<CanvasGroup>();
            string cgInfo = canvasGroup != null ? $" CanvasGroup[alpha={canvasGroup.alpha}]" : "";
            
            var img = go.GetComponent<UnityEngine.UI.Image>();
            string imgInfo = img != null ? $" Image[enabled={img.enabled}, color={img.color}, sprite={(img.sprite != null ? img.sprite.name : "null")}]" : "";
            
            var txt = go.GetComponent<TMPro.TextMeshProUGUI>();
            string txtInfo = txt != null ? $" TMP[enabled={txt.enabled}, text='{txt.text}', color={txt.color}, alpha={txt.color.a}]" : "";

            sb.AppendLine($"- {go.name}: active={go.activeInHierarchy} (self={go.activeSelf}), scale={r.localScale}, pos={r.anchoredPosition}, size={r.sizeDelta}, anchors={r.anchorMin}-{r.anchorMax}{cgInfo}{imgInfo}{txtInfo}");
        }

        File.WriteAllText("Assets/Editor/visibility_log.txt", sb.ToString());
        Debug.Log("Visibility log written to Assets/Editor/visibility_log.txt");
    }
}
