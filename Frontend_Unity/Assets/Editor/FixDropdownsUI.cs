using UnityEngine;
using UnityEditor;
using TMPro;

public class FixDropdownsUI
{
    [MenuItem("Tools/Fix Device Dropdowns")]
    public static void FixDropdowns()
    {
        // GameObject.Find no encuentra inactivos, buscaremos el WebcamSender primero
        WebcamSender sender = GameObject.FindObjectOfType<WebcamSender>(true);
        if (sender == null)
        {
            Debug.LogError("No se encontro el script WebcamSender en la escena.");
            return;
        }

        GameObject panelEntrevista = null;
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach(Transform t in allTransforms)
        {
            if (t.name == "PanelEntrevista" && t.gameObject.scene.isLoaded)
            {
                panelEntrevista = t.gameObject;
                break;
            }
        }

        if (panelEntrevista == null)
        {
            Debug.LogError("No se encontro PanelEntrevista (ni activo ni inactivo).");
            return;
        }

        // Delete old broken ones
        Transform oldCam = panelEntrevista.transform.Find("DropdownCamara");
        if (oldCam != null) Object.DestroyImmediate(oldCam.gameObject);
        
        Transform oldMic = panelEntrevista.transform.Find("DropdownMicrofono");
        if (oldMic != null) Object.DestroyImmediate(oldMic.gameObject);

        // Load the actual TMP Dropdown prefab or use TMP_DefaultControls
        var resources = new TMPro.TMP_DefaultControls.Resources();
        
        GameObject dropCam = TMPro.TMP_DefaultControls.CreateDropdown(resources);
        dropCam.name = "DropdownCamara";
        dropCam.transform.SetParent(panelEntrevista.transform, false);

        GameObject dropMic = TMPro.TMP_DefaultControls.CreateDropdown(resources);
        dropMic.name = "DropdownMicrofono";
        dropMic.transform.SetParent(panelEntrevista.transform, false);

        RectTransform rtCam = dropCam.GetComponent<RectTransform>();
        rtCam.anchorMin = new Vector2(0, 1);
        rtCam.anchorMax = new Vector2(0, 1);
        rtCam.pivot = new Vector2(0, 1);
        rtCam.anchoredPosition = new Vector2(20, -20);
        rtCam.sizeDelta = new Vector2(200, 30);

        RectTransform rtMic = dropMic.GetComponent<RectTransform>();
        rtMic.anchorMin = new Vector2(0, 1);
        rtMic.anchorMax = new Vector2(0, 1);
        rtMic.pivot = new Vector2(0, 1);
        rtMic.anchoredPosition = new Vector2(20, -60);
        rtMic.sizeDelta = new Vector2(200, 30);

        if (sender != null)
        {
            sender.dropdownCamara = dropCam.GetComponent<TMP_Dropdown>();
            sender.dropdownMicrofono = dropMic.GetComponent<TMP_Dropdown>();
            EditorUtility.SetDirty(sender);
            Debug.Log("Dropdowns COMPLETOS (con todo el UI visual) creados y asignados al WebcamSender.");
        }
    }
}
