using UnityEngine;
using UnityEditor;
using TMPro;

public class AddDropdownsUI
{
    [MenuItem("Tools/Add Device Dropdowns")]
    public static void AddDropdowns()
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

        // Crear dropdown de camara (usando prefab por defecto si es posible, o clonando uno de UI)
        // Para TextMeshPro, lo mas sencillo es usar ObjectFactory
        GameObject dropCam = ObjectFactory.CreateGameObject("DropdownCamara", typeof(RectTransform), typeof(TMP_Dropdown));
        dropCam.transform.SetParent(panelEntrevista.transform, false);
        RectTransform rtCam = dropCam.GetComponent<RectTransform>();
        rtCam.anchorMin = new Vector2(0, 1);
        rtCam.anchorMax = new Vector2(0, 1);
        rtCam.pivot = new Vector2(0, 1);
        rtCam.anchoredPosition = new Vector2(20, -20);
        rtCam.sizeDelta = new Vector2(200, 30);

        GameObject dropMic = ObjectFactory.CreateGameObject("DropdownMicrofono", typeof(RectTransform), typeof(TMP_Dropdown));
        dropMic.transform.SetParent(panelEntrevista.transform, false);
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
            Debug.Log("Dropdowns creados y asignados al WebcamSender. Nota: Debes asignarles los componentes visuales (Template, Label) desde el editor manualmente, o usar un prefab.");
        }
    }
}
