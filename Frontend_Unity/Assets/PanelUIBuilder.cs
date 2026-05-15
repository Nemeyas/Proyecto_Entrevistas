using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Construye programáticamente los paneles de Reporte y de Historial.
/// Se añade automáticamente al mismo objeto que MenuAutoBuilder.
/// </summary>
public class PanelUIBuilder : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panelReporte;
    private GameObject panelHistorial;
    private GameObject panelDetalleReporte;

    private string urlHistorial = "http://localhost:8000/historial_reportes";
    private string urlReporte = "http://localhost:8000/reporte/";

    void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
    }

    // =========================================================
    //  PANEL DE REPORTE (se muestra al finalizar la entrevista)
    // =========================================================
    public void MostrarReporte(DatosReporte reporte)
    {
        if (panelReporte != null) Destroy(panelReporte);

        panelReporte = CrearPanelBase("PanelReporte");

        // Tarjeta central scrolleable
        GameObject tarjeta = CrearTarjeta(panelReporte.transform, 600, 550);

        // Scroll View dentro de la tarjeta
        GameObject scrollArea = CrearScrollView(tarjeta.transform);
        Transform contenido = scrollArea.transform.GetChild(0).GetChild(0); // Viewport > Content

        // Título
        CrearLabel(contenido, "REPORTE DE ENTREVISTA", 24, Color.white, FontStyle.Bold, 40);
        CrearEspaciador(contenido, 5);

        // Puntaje grande
        string nota = reporte.puntaje >= 90 ? "A" : reporte.puntaje >= 75 ? "B" : reporte.puntaje >= 60 ? "C" : reporte.puntaje >= 40 ? "D" : "F";
        Color colorNota = reporte.puntaje >= 75 ? new Color(0.3f, 0.85f, 0.4f) : reporte.puntaje >= 60 ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
        CrearLabel(contenido, $"{reporte.puntaje}/100  ({nota})", 36, colorNota, FontStyle.Bold, 55);
        CrearEspaciador(contenido, 5);

        // Resumen
        CrearLabel(contenido, "RESUMEN", 16, new Color(0.5f, 0.7f, 1f), FontStyle.Bold, 25);
        CrearLabel(contenido, reporte.resumen ?? "Sin datos", 14, new Color(0.85f, 0.85f, 0.9f), FontStyle.Normal, 0, true);
        CrearEspaciador(contenido, 8);

        // Estado Emocional
        CrearLabel(contenido, "ESTADO EMOCIONAL", 16, new Color(0.5f, 0.7f, 1f), FontStyle.Bold, 25);
        CrearLabel(contenido, reporte.estado_emocional ?? "Sin datos", 14, new Color(0.85f, 0.85f, 0.9f), FontStyle.Normal, 0, true);
        CrearEspaciador(contenido, 8);

        // Recomendaciones
        CrearLabel(contenido, "RECOMENDACIONES", 16, new Color(0.5f, 0.7f, 1f), FontStyle.Bold, 25);
        if (reporte.recomendaciones != null)
        {
            foreach (var r in reporte.recomendaciones)
                CrearLabel(contenido, "• " + r, 13, new Color(0.8f, 0.8f, 0.85f), FontStyle.Normal, 0, true);
        }
        CrearEspaciador(contenido, 8);

        // Momentos Críticos
        CrearLabel(contenido, "MOMENTOS CRÍTICOS", 16, new Color(1f, 0.6f, 0.4f), FontStyle.Bold, 25);
        if (reporte.momentos_criticos != null && reporte.momentos_criticos.Count > 0)
        {
            foreach (var mc in reporte.momentos_criticos)
                CrearLabel(contenido, $"Q: {mc.pregunta}\nObs: {mc.observacion}", 13, new Color(0.8f, 0.75f, 0.7f), FontStyle.Normal, 0, true);
        }
        else
        {
            CrearLabel(contenido, "Ninguno detectado.", 13, new Color(0.6f, 0.8f, 0.6f), FontStyle.Normal, 25);
        }

        CrearEspaciador(contenido, 15);

        // Botón Volver al Menú
        Button btnVolver = CrearBoton(contenido, "Volver al Menú", new Color(0.2f, 0.5f, 0.8f), 45);
        btnVolver.onClick.AddListener(() => {
            Destroy(panelReporte);
            // Recrear el menú
            MenuAutoBuilder menu = FindObjectOfType<MenuAutoBuilder>();
            if (menu == null) menu = gameObject.AddComponent<MenuAutoBuilder>();
            else menu.ReconstruirMenu();
        });
    }

    // =========================================================
    //  PANEL DE HISTORIAL
    // =========================================================
    public void MostrarHistorial()
    {
        if (panelHistorial != null) Destroy(panelHistorial);

        panelHistorial = CrearPanelBase("PanelHistorial");

        GameObject tarjeta = CrearTarjeta(panelHistorial.transform, 650, 500);

        // Título
        CrearLabel(tarjeta.transform, "HISTORIAL DE ENTREVISTAS", 22, Color.white, FontStyle.Bold, 40);

        // Texto de carga
        Text txtCargando = CrearLabel(tarjeta.transform, "Cargando...", 16, new Color(0.7f, 0.7f, 0.8f), FontStyle.Italic, 30);

        // Scroll para las tarjetas
        GameObject scrollArea = CrearScrollView(tarjeta.transform);
        Transform contenido = scrollArea.transform.GetChild(0).GetChild(0);

        // Botón Volver
        Button btnVolver = CrearBoton(tarjeta.transform, "Volver al Menú", new Color(0.3f, 0.35f, 0.5f), 42);
        btnVolver.onClick.AddListener(() => {
            Destroy(panelHistorial);
            MenuAutoBuilder menu = FindObjectOfType<MenuAutoBuilder>();
            if (menu == null) menu = gameObject.AddComponent<MenuAutoBuilder>();
            else menu.ReconstruirMenu();
        });

        StartCoroutine(CargarHistorial(contenido, txtCargando));
    }

    IEnumerator CargarHistorial(Transform contenido, Text txtCargando)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(urlHistorial))
        {
            yield return www.SendWebRequest();
            if (txtCargando != null) Destroy(txtCargando.gameObject);

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                HistorialResponse resp = JsonUtility.FromJson<HistorialResponse>(json);

                if (resp.status == "exito" && resp.historial != null && resp.historial.Count > 0)
                {
                    foreach (var item in resp.historial)
                    {
                        CrearTarjetaHistorial(contenido, item);
                    }
                }
                else
                {
                    CrearLabel(contenido, "No hay entrevistas registradas.", 15, new Color(0.7f, 0.7f, 0.7f), FontStyle.Italic, 40);
                }
            }
            else
            {
                CrearLabel(contenido, "Error de conexión con el servidor.", 15, new Color(1f, 0.4f, 0.4f), FontStyle.Normal, 40);
            }
        }
    }

    void CrearTarjetaHistorial(Transform padre, HistorialItem item)
    {
        GameObject fila = new GameObject("Fila_" + item.IDSimulacion);
        fila.transform.SetParent(padre, false);
        RectTransform rtF = fila.AddComponent<RectTransform>();

        Image bgFila = fila.AddComponent<Image>();
        bgFila.color = new Color(0.14f, 0.16f, 0.26f, 0.9f);

        LayoutElement leF = fila.AddComponent<LayoutElement>();
        leF.preferredHeight = 75;
        leF.flexibleWidth = 1;

        HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Info
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(fila.transform, false);
        LayoutElement leInfo = infoObj.AddComponent<LayoutElement>();
        leInfo.flexibleWidth = 1;

        VerticalLayoutGroup vlgInfo = infoObj.AddComponent<VerticalLayoutGroup>();
        vlgInfo.childForceExpandHeight = true;

        string nombre = string.IsNullOrEmpty(item.NombrePostulante) ? "Sin nombre" : item.NombrePostulante;
        CrearLabel(infoObj.transform, $"{nombre}  |  {item.Dificultad}", 14, Color.white, FontStyle.Bold, 20);
        CrearLabel(infoObj.transform, $"Fecha: {item.TiempoInicio}", 12, new Color(0.6f, 0.6f, 0.7f), FontStyle.Normal, 18);

        // Puntaje
        Color cP = item.PuntajeGlobal >= 75 ? new Color(0.3f, 0.85f, 0.4f) : item.PuntajeGlobal >= 60 ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
        Text txtPuntaje = CrearLabel(fila.transform, $"{item.PuntajeGlobal}", 22, cP, FontStyle.Bold, 0);
        LayoutElement lePuntaje = txtPuntaje.gameObject.AddComponent<LayoutElement>();
        lePuntaje.preferredWidth = 55;

        // Botón ver
        Button btnVer = CrearBoton(fila.transform, "Ver", new Color(0.2f, 0.5f, 0.8f), 0);
        LayoutElement leBtnVer = btnVer.gameObject.AddComponent<LayoutElement>();
        leBtnVer.preferredWidth = 65;

        int idSim = item.IDSimulacion;
        btnVer.onClick.AddListener(() => {
            StartCoroutine(CargarDetalleReporte(idSim));
        });
    }

    IEnumerator CargarDetalleReporte(int idSimulacion)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(urlReporte + idSimulacion))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                ReporteDetailResponse resp = JsonUtility.FromJson<ReporteDetailResponse>(www.downloadHandler.text);
                if (resp.status == "exito" && resp.reporte != null && resp.reporte.Resumen_JSON != null)
                {
                    if (panelHistorial != null) Destroy(panelHistorial);
                    MostrarReporte(resp.reporte.Resumen_JSON);
                }
            }
        }
    }

    // =========================================================
    //  HELPERS DE CONSTRUCCIÓN UI
    // =========================================================

    GameObject CrearPanelBase(string nombre)
    {
        GameObject panel = new GameObject(nombre);
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.10f, 1f);
        return panel;
    }

    GameObject CrearTarjeta(Transform padre, float ancho, float alto)
    {
        GameObject tarjeta = new GameObject("Tarjeta");
        tarjeta.transform.SetParent(padre, false);

        RectTransform rt = tarjeta.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(ancho, alto);
        rt.anchoredPosition = Vector2.zero;

        Image img = tarjeta.AddComponent<Image>();
        img.color = new Color(0.10f, 0.12f, 0.20f, 0.97f);

        VerticalLayoutGroup vlg = tarjeta.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 15, 15);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return tarjeta;
    }

    GameObject CrearScrollView(Transform padre)
    {
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(padre, false);

        RectTransform rtScroll = scrollObj.AddComponent<RectTransform>();
        LayoutElement leScroll = scrollObj.AddComponent<LayoutElement>();
        leScroll.flexibleHeight = 1;
        leScroll.flexibleWidth = 1;

        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.08f, 0.09f, 0.15f, 0.5f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform rtVP = viewport.AddComponent<RectTransform>();
        rtVP.anchorMin = Vector2.zero;
        rtVP.anchorMax = Vector2.one;
        rtVP.offsetMin = Vector2.zero;
        rtVP.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = Color.clear;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform rtContent = content.AddComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0, 1);
        rtContent.anchorMax = new Vector2(1, 1);
        rtContent.pivot = new Vector2(0.5f, 1);
        rtContent.offsetMin = Vector2.zero;
        rtContent.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlgC = content.AddComponent<VerticalLayoutGroup>();
        vlgC.padding = new RectOffset(10, 10, 5, 5);
        vlgC.spacing = 6;
        vlgC.childAlignment = TextAnchor.UpperCenter;
        vlgC.childForceExpandWidth = true;
        vlgC.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = rtVP;
        sr.content = rtContent;

        return scrollObj;
    }

    public Text CrearLabel(Transform padre, string texto, int size, Color color, FontStyle estilo, float altura, bool autoSize = false)
    {
        GameObject obj = new GameObject("Txt");
        obj.transform.SetParent(padre, false);

        Text t = obj.AddComponent<Text>();
        t.text = texto;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = estilo;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", size);

        if (autoSize)
        {
            ContentSizeFitter csf = obj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else if (altura > 0)
        {
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = altura;
        }

        return t;
    }

    public Button CrearBoton(Transform padre, string texto, Color colorFondo, float altura)
    {
        GameObject btnObj = new GameObject(texto);
        btnObj.transform.SetParent(padre, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = colorFondo;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = colorFondo * 1.2f;
        cb.pressedColor = colorFondo * 0.8f;
        btn.colors = cb;

        if (altura > 0)
        {
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = altura;
        }

        // Texto del botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text = texto;
        t.fontSize = 16;
        t.color = Color.white;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", 16);

        RectTransform rtT = textObj.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero;
        rtT.offsetMax = Vector2.zero;

        return btn;
    }

    void CrearEspaciador(Transform padre, float altura)
    {
        GameObject obj = new GameObject("Sp");
        obj.transform.SetParent(padre, false);
        obj.AddComponent<RectTransform>();
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = altura;
    }
}
