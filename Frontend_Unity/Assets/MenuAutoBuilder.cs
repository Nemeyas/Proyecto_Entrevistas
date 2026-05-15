using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class InicioResponse
{
    public string status;
    public int id_simulacion;
    public string mensaje;
}

/// <summary>
/// Crea el menú principal de forma automática al iniciar la escena.
/// Se destruye a sí mismo después de que el usuario selecciona un modo.
/// </summary>
public class MenuAutoBuilder : MonoBehaviour
{
    private GameObject panelMenu;
    private InputField campoNombre;
    private InputField campoRUT;
    private Text textoError;
    private Button btnPasivo;
    private Button btnAgresivo;

    private string urlIniciar = "http://localhost:8000/iniciar_entrevista";

    void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        CrearMenu(canvas.transform);
    }

    /// <summary>
    /// Permite recrear el menú desde otros scripts (ej: al volver del reporte).
    /// </summary>
    public void ReconstruirMenu()
    {
        if (panelMenu != null) Destroy(panelMenu);
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        CrearMenu(canvas.transform);
    }

    void CrearMenu(Transform padre)
    {
        // ========== PANEL FONDO (pantalla completa, oscuro) ==========
        panelMenu = new GameObject("PanelMenuAuto");
        panelMenu.transform.SetParent(padre, false);
        panelMenu.transform.SetAsLastSibling();

        RectTransform rt = panelMenu.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image fondo = panelMenu.AddComponent<Image>();
        fondo.color = new Color(0.05f, 0.06f, 0.10f, 1f);

        // ========== TARJETA CENTRAL ==========
        GameObject tarjeta = CrearObjeto("Tarjeta", panelMenu.transform);
        RectTransform rtT = tarjeta.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0.5f, 0.5f);
        rtT.anchorMax = new Vector2(0.5f, 0.5f);
        rtT.sizeDelta = new Vector2(520, 530);
        rtT.anchoredPosition = Vector2.zero;

        Image imgTarjeta = tarjeta.AddComponent<Image>();
        imgTarjeta.color = new Color(0.10f, 0.12f, 0.20f, 0.97f);

        VerticalLayoutGroup vlg = tarjeta.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(35, 35, 30, 30);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ========== TITULO ==========
        CrearLabel(tarjeta.transform, "Simulador de Entrevista IA", 26, Color.white, FontStyle.Bold, 45);
        CrearLabel(tarjeta.transform, "Ingrese sus datos para comenzar", 15,
            new Color(0.65f, 0.65f, 0.75f), FontStyle.Italic, 25);

        CrearEspaciador(tarjeta.transform, 8);

        // ========== CAMPO NOMBRE ==========
        CrearLabel(tarjeta.transform, "Nombre Completo", 14, new Color(0.8f, 0.8f, 0.9f), FontStyle.Normal, 22);
        campoNombre = CrearInput(tarjeta.transform, "Ingrese su nombre...");

        // ========== CAMPO RUT ==========
        CrearLabel(tarjeta.transform, "RUT", 14, new Color(0.8f, 0.8f, 0.9f), FontStyle.Normal, 22);
        campoRUT = CrearInput(tarjeta.transform, "Ej: 12345678-9");

        CrearEspaciador(tarjeta.transform, 10);

        // ========== BOTONES DE MODO ==========
        GameObject filaBotones = CrearObjeto("FilaBotones", tarjeta.transform);
        LayoutElement leFila = filaBotones.AddComponent<LayoutElement>();
        leFila.preferredHeight = 50;

        HorizontalLayoutGroup hlg = filaBotones.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        btnPasivo = CrearBoton(filaBotones.transform, "Modo Pasivo", new Color(0.15f, 0.45f, 0.75f));
        btnAgresivo = CrearBoton(filaBotones.transform, "Modo Agresivo", new Color(0.75f, 0.18f, 0.25f));

        btnPasivo.onClick.AddListener(() => OnClickModo("pasivo"));
        btnAgresivo.onClick.AddListener(() => OnClickModo("agresivo"));

        CrearEspaciador(tarjeta.transform, 8);

        // ========== BOTÓN VER HISTORIAL ==========
        Button btnHistorial = CrearBoton(tarjeta.transform, "Ver Historial de Entrevistas", new Color(0.25f, 0.3f, 0.45f));
        LayoutElement leHist = btnHistorial.gameObject.AddComponent<LayoutElement>();
        leHist.preferredHeight = 42;
        btnHistorial.onClick.AddListener(() => {
            Destroy(panelMenu);
            PanelUIBuilder builder = GetOrCreatePanelBuilder();
            builder.MostrarHistorial();
        });

        // ========== TEXTO ERROR ==========
        textoError = CrearLabel(tarjeta.transform, "", 13, new Color(1f, 0.4f, 0.4f), FontStyle.Normal, 25);
    }

    // ===================== EVENTOS =====================

    void OnClickModo(string modo)
    {
        string nombre = campoNombre.text.Trim();
        string rut = campoRUT.text.Trim();

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(rut))
        {
            textoError.text = "Por favor ingrese Nombre y RUT.";
            return;
        }

        textoError.text = "Conectando al servidor...";
        btnPasivo.interactable = false;
        btnAgresivo.interactable = false;

        StartCoroutine(EnviarInicio(rut, nombre, modo));
    }

    IEnumerator EnviarInicio(string rut, string nombre, string dificultad)
    {
        WWWForm form = new WWWForm();
        form.AddField("id_postulante", rut);
        form.AddField("nombre_postulante", nombre);
        form.AddField("dificultad", dificultad);

        using (UnityWebRequest www = UnityWebRequest.Post(urlIniciar, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                InicioResponse resp = JsonUtility.FromJson<InicioResponse>(www.downloadHandler.text);

                if (resp.status == "exito")
                {
                    // Guardar datos en GestorNavegacion si existe
                    if (GestorNavegacion.Instancia == null)
                    {
                        GameObject navObj = new GameObject("GestorNavegacion");
                        GestorNavegacion nav = navObj.AddComponent<GestorNavegacion>();
                        nav.idSimulacionActiva = resp.id_simulacion;
                        nav.nombrePostulanteActivo = nombre;
                        nav.rutPostulanteActivo = rut;
                    }
                    else
                    {
                        GestorNavegacion.Instancia.idSimulacionActiva = resp.id_simulacion;
                        GestorNavegacion.Instancia.nombrePostulanteActivo = nombre;
                        GestorNavegacion.Instancia.rutPostulanteActivo = rut;
                    }

                    // Destruir menú e iniciar entrevista
                    Destroy(panelMenu);
                    WebcamSender ws = FindObjectOfType<WebcamSender>();
                    if (ws != null) ws.IniciarEntrevista();
                }
                else
                {
                    textoError.text = "Error: " + resp.mensaje;
                    btnPasivo.interactable = true;
                    btnAgresivo.interactable = true;
                }
            }
            else
            {
                textoError.text = "Sin conexión al servidor. ¿Está encendido?";
                btnPasivo.interactable = true;
                btnAgresivo.interactable = true;
            }
        }
    }

    PanelUIBuilder GetOrCreatePanelBuilder()
    {
        PanelUIBuilder builder = FindObjectOfType<PanelUIBuilder>();
        if (builder == null) builder = gameObject.AddComponent<PanelUIBuilder>();
        return builder;
    }

    // ===================== HELPERS UI =====================

    GameObject CrearObjeto(string nombre, Transform padre)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    Text CrearLabel(Transform padre, string texto, int size, Color color, FontStyle estilo, float altura)
    {
        GameObject obj = new GameObject("Label");
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

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = altura;

        return t;
    }

    InputField CrearInput(Transform padre, string placeholder)
    {
        GameObject container = new GameObject("InputField");
        container.transform.SetParent(padre, false);

        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.20f, 0.28f, 1f);

        LayoutElement le = container.AddComponent<LayoutElement>();
        le.preferredHeight = 40;

        InputField input = container.AddComponent<InputField>();

        // Texto escrito
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(container.transform, false);
        Text textComp = textObj.AddComponent<Text>();
        textComp.color = Color.white;
        textComp.fontSize = 16;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (textComp.font == null) textComp.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        textComp.supportRichText = false;

        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = new Vector2(10, 2);
        rtText.offsetMax = new Vector2(-10, -2);

        // Placeholder
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(container.transform, false);
        Text phComp = phObj.AddComponent<Text>();
        phComp.text = placeholder;
        phComp.color = new Color(0.5f, 0.5f, 0.6f);
        phComp.fontSize = 16;
        phComp.fontStyle = FontStyle.Italic;
        phComp.font = textComp.font;

        RectTransform rtPh = phObj.GetComponent<RectTransform>();
        rtPh.anchorMin = Vector2.zero;
        rtPh.anchorMax = Vector2.one;
        rtPh.offsetMin = new Vector2(10, 2);
        rtPh.offsetMax = new Vector2(-10, -2);

        input.textComponent = textComp;
        input.placeholder = phComp;

        return input;
    }

    Button CrearBoton(Transform padre, string texto, Color colorFondo)
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

        // Texto del botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text = texto;
        t.fontSize = 18;
        t.color = Color.white;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", 18);

        RectTransform rtT = textObj.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero;
        rtT.offsetMax = Vector2.zero;

        return btn;
    }

    void CrearEspaciador(Transform padre, float altura)
    {
        GameObject obj = new GameObject("Espaciador");
        obj.transform.SetParent(padre, false);
        obj.AddComponent<RectTransform>();
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = altura;
    }
}
