using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogoTerminoPrematuro : MonoBehaviour
{
    /// <summary>
    /// Crea y muestra un diálogo modal en pantalla con diseño claro y de alto contraste.
    /// </summary>
    public static DialogoTerminoPrematuro Mostrar(Action alGuardar, Action alSalir, Action alCancelar)
    {
        // 1. Encontrar el Canvas principal en la escena
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DialogoTerminoPrematuro] No se encontró ningún Canvas en la escena.");
            return null;
        }

        // 2. Crear el panel de fondo modal
        GameObject modalBg = new GameObject("ModalConfirmacionTermino", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalBg.transform.SetParent(canvas.transform, false);
        
        RectTransform rtBg = modalBg.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.sizeDelta = Vector2.zero;
        rtBg.anchoredPosition = Vector2.zero;
        
        Image imgBg = modalBg.GetComponent<Image>();
        imgBg.color = new Color(0f, 0f, 0f, 0.75f); // Fondo oscuro semi-transparente premium
        
        // Obtener la fuente de TextMeshPro activa en la escena para prevenir el color negro por defecto de TMPro
        TMP_FontAsset font = null;
        TextMeshProUGUI activeTmp = FindObjectOfType<TextMeshProUGUI>();
        if (activeTmp != null)
        {
            font = activeTmp.font;
        }

        // 3. Crear la caja contenedora del diálogo
        GameObject caja = new GameObject("CajaDialogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        caja.transform.SetParent(modalBg.transform, false);
        
        RectTransform rtCaja = caja.GetComponent<RectTransform>();
        rtCaja.sizeDelta = new Vector2(500, 240);
        rtCaja.anchorMin = new Vector2(0.5f, 0.5f);
        rtCaja.anchorMax = new Vector2(0.5f, 0.5f);
        rtCaja.anchoredPosition = Vector2.zero;
        
        Image imgCaja = caja.GetComponent<Image>();
        imgCaja.color = new Color(0.95f, 0.96f, 0.98f, 1f); // Gris muy claro premium y moderno (light theme)
        
        // Agregar un borde sutil al panel
        Outline outline = caja.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.82f, 0.86f, 1f); // Borde suave claro
        outline.effectDistance = new Vector2(2, -2);
        
        // 4. Título del diálogo
        GameObject tituloObj = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tituloObj.transform.SetParent(caja.transform, false);
        RectTransform rtTitulo = tituloObj.GetComponent<RectTransform>();
        rtTitulo.anchorMin = new Vector2(0, 1);
        rtTitulo.anchorMax = new Vector2(1, 1);
        rtTitulo.pivot = new Vector2(0.5f, 1);
        rtTitulo.anchoredPosition = new Vector2(0, -20);
        rtTitulo.sizeDelta = new Vector2(-40, 40);
        
        TextMeshProUGUI txtTitulo = tituloObj.GetComponent<TextMeshProUGUI>();
        if (font != null) txtTitulo.font = font;
        txtTitulo.text = "¿Terminar entrevista prematuramente?";
        txtTitulo.fontSize = 20;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.alignment = TextAlignmentOptions.Center;
        txtTitulo.color = new Color(0.06f, 0.09f, 0.15f, 1f); // Azul/Gris oscuro de alta legibilidad
        
        // 5. Descripción
        GameObject descObj = new GameObject("Descripcion", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(caja.transform, false);
        RectTransform rtDesc = descObj.GetComponent<RectTransform>();
        rtDesc.anchorMin = new Vector2(0, 0.5f);
        rtDesc.anchorMax = new Vector2(1, 0.5f);
        rtDesc.pivot = new Vector2(0.5f, 0.5f);
        rtDesc.anchoredPosition = new Vector2(0, 10);
        rtDesc.sizeDelta = new Vector2(-40, 85);
        
        TextMeshProUGUI txtDesc = descObj.GetComponent<TextMeshProUGUI>();
        if (font != null) txtDesc.font = font;
        txtDesc.text = "La entrevista aún no concluye. ¿Qué deseas hacer?\n\n<b>Guardar Parcial</b>: Genera el reporte con lo respondido.\n<b>Salir sin Guardar</b>: Descarta esta sesión del historial.";
        txtDesc.fontSize = 13;
        txtDesc.alignment = TextAlignmentOptions.Center;
        txtDesc.color = new Color(0.22f, 0.25f, 0.32f, 1f); // Gris pizarra medio para el texto instructivo
        
        // 6. Crear los tres botones de opción en la parte inferior
        // Botón 1: Guardar Parcial (Verde)
        CrearBoton(caja.transform, new Vector2(-155, -70), new Vector2(130, 40), "Guardar Parcial", new Color(0.18f, 0.55f, 0.34f), () => {
            Destroy(modalBg);
            alGuardar?.Invoke();
        }, font);
        
        // Botón 2: Salir sin Guardar (Rojo)
        CrearBoton(caja.transform, new Vector2(0, -70), new Vector2(130, 40), "Salir sin Guardar", new Color(0.75f, 0.22f, 0.22f), () => {
            Destroy(modalBg);
            alSalir?.Invoke();
        }, font);
        
        // Botón 3: Cancelar (Gris)
        CrearBoton(caja.transform, new Vector2(155, -70), new Vector2(100, 40), "Cancelar", new Color(0.45f, 0.45f, 0.48f), () => {
            Destroy(modalBg);
            alCancelar?.Invoke();
        }, font);
        
        return modalBg.AddComponent<DialogoTerminoPrematuro>();
    }
    
    private static Button CrearBoton(Transform parent, Vector2 pos, Vector2 size, string texto, Color colorBase, Action onClickAction, TMP_FontAsset font)
    {
        GameObject btnObj = new GameObject($"Boton_{texto}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        
        Image img = btnObj.GetComponent<Image>();
        img.color = colorBase;
        
        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = colorBase;
        cb.highlightedColor = colorBase * 1.15f;
        cb.pressedColor = colorBase * 0.8f;
        btn.colors = cb;
        
        btn.onClick.AddListener(() => onClickAction?.Invoke());
        
        // Texto
        GameObject txtObj = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI txtComp = txtObj.GetComponent<TextMeshProUGUI>();
        if (font != null) txtComp.font = font;
        txtComp.text = texto;
        txtComp.fontSize = 11;
        txtComp.fontStyle = FontStyles.Bold;
        txtComp.alignment = TextAlignmentOptions.Center;
        txtComp.color = Color.white; // Texto blanco de alto contraste sobre fondo del botón
        
        return btn;
    }
}
