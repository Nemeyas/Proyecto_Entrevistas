using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MomentoCritico
{
    public string pregunta;
    public string observacion;
}

[System.Serializable]
public class DatosReporte
{
    public float puntaje;
    public string resumen;
    public string estado_emocional;
    public List<string> recomendaciones;
    public List<MomentoCritico> momentos_criticos;
}

[System.Serializable]
public class ResponseReporteBackend
{
    public string status;
    public string mensaje;
    public DatosReporte reporte;
}

public class GestorReporte : MonoBehaviour
{
    public TextMeshProUGUI textoPuntaje;
    public TextMeshProUGUI textoNotaLetra;
    public TextMeshProUGUI textoResumen;
    public TextMeshProUGUI textoEstadoEmocional;
    public TextMeshProUGUI textoRecomendaciones;
    public TextMeshProUGUI textoMomentosCriticos;
    public Button btnSalir;

    [Header("UI Momentos Críticos Dinámicos")]
    public Transform contenedorMomentosCriticos;
    public GameObject plantillaMomentoCritico;

    private bool uiDesplazada = false;

    void Start()
    {
        if (btnSalir != null)
        {
            btnSalir.onClick.AddListener(() => {
                GestorNavegacion.Instancia.MostrarMenu();
            });
        }
    }

    public void MostrarDatosReporte(DatosReporte reporte)
    {
        if (reporte == null) return;
        // --- Adaptar el tamaño de las cajas EXACTAMENTE al texto ---
        // 1. Forzar a todos los textos a actualizarse para tener las medidas reales
        TextMeshProUGUI[] todosTextos = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in todosTextos)
        {
            txt.ForceMeshUpdate(true, true);
        }
        Canvas.ForceUpdateCanvases(); 

        System.Func<Component, float, float> CalcularAlturaPanel = (comp, padding) => {
            if (comp == null) return 0f;
            float alturaContenido = 0f;
            TextMeshProUGUI txt = comp as TextMeshProUGUI;
            if (txt != null)
            {
                alturaContenido = txt.preferredHeight;
            }
            else
            {
                Transform t = comp as Transform;
                foreach (Transform child in t)
                {
                    if (child.gameObject.activeSelf)
                    {
                        RectTransform childRect = child.GetComponent<RectTransform>();
                        if (childRect != null) 
                        {
                            float childHeight = UnityEngine.UI.LayoutUtility.GetPreferredHeight(childRect);
                            if (childHeight <= 0) childHeight = childRect.rect.height;
                            
                            // Revisar también los textos internos por si el layoutUtility falla
                            TextMeshProUGUI[] textosHijos = child.GetComponentsInChildren<TextMeshProUGUI>();
                            float sumTextos = 0f;
                            foreach(var th in textosHijos) sumTextos += th.preferredHeight + 10f;
                            
                            if (sumTextos > childHeight) childHeight = sumTextos;

                            alturaContenido += childHeight + 15f;
                        }
                    }
                }
            }
            return alturaContenido + padding;
        };

        System.Action<Component, float> AplicarAlturaPanel = (comp, altura) => {
            if (comp == null || altura <= 0) return;
            Image fondo = comp.GetComponentInParent<Image>();
            if (fondo != null)
            {
                RectTransform rectFondo = fondo.GetComponent<RectTransform>();
                rectFondo.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, altura);
                
                var le = fondo.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le == null) le = fondo.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                le.minHeight = altura;
            }
        };

        float paddingEstandar = 100f; 

        // 2. Calcular alturas necesarias
        float altResumen = CalcularAlturaPanel(textoResumen, paddingEstandar);
        float altEmocional = CalcularAlturaPanel(textoEstadoEmocional, paddingEstandar);
        float altRec = CalcularAlturaPanel(textoRecomendaciones, paddingEstandar + 20f);
        
        Component criticosComp = contenedorMomentosCriticos != null ? (Component)contenedorMomentosCriticos : textoMomentosCriticos;
        float altCrit = CalcularAlturaPanel(criticosComp, paddingEstandar + 20f);

        // 3. Altura idéntica para los cuadros inferiores
        float altMaximaInferiores = Mathf.Max(altRec, altCrit);

        AplicarAlturaPanel(textoResumen, altResumen);
        AplicarAlturaPanel(textoEstadoEmocional, altEmocional);
        AplicarAlturaPanel(textoRecomendaciones, altMaximaInferiores);
        AplicarAlturaPanel(criticosComp, altMaximaInferiores);

        Canvas.ForceUpdateCanvases(); 

        // 4. Posicionamiento Global Absoluto para evitar solapamientos
        System.Action<RectTransform, float> MoverBordeSuperiorA = (rect, targetWorldY) => {
            if (rect == null) return;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float topWorldY = corners[1].y; // Esquina superior izquierda
            float diff = targetWorldY - topWorldY;
            rect.position += new Vector3(0, diff, 0);
        };

        System.Func<RectTransform, float> ObtenerBordeInferior = (rect) => {
            if (rect == null) return 0f;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[0].y; // Esquina inferior izquierda
        };

        Image fRes = textoResumen != null ? textoResumen.GetComponentInParent<Image>() : null;
        Image fEmo = textoEstadoEmocional != null ? textoEstadoEmocional.GetComponentInParent<Image>() : null;
        Image fRec = textoRecomendaciones != null ? textoRecomendaciones.GetComponentInParent<Image>() : null;
        Image fCrit = criticosComp != null ? criticosComp.GetComponentInParent<Image>() : null;

        // Utilizamos la escala del canvas para definir el gap visual (aprox 30 pixeles locales)
        float worldGap = 30f;
        if (fRes != null)
        {
            Canvas canvas = fRes.GetComponentInParent<Canvas>();
            if (canvas != null) worldGap = 30f * canvas.transform.localScale.y;
        }

        if (fRes != null && fEmo != null)
        {
            float bottomResumen = ObtenerBordeInferior(fRes.GetComponent<RectTransform>());
            MoverBordeSuperiorA(fEmo.GetComponent<RectTransform>(), bottomResumen - worldGap);
        }

        if (fEmo != null && fRec != null)
        {
            float bottomEmocional = ObtenerBordeInferior(fEmo.GetComponent<RectTransform>());
            MoverBordeSuperiorA(fRec.GetComponent<RectTransform>(), bottomEmocional - worldGap);
        }

        if (fRec != null && fCrit != null)
        {
            // Alinear Momentos Críticos exactamente a la misma altura que Recomendaciones
            Vector3 posRec = fRec.transform.position;
            Vector3 posCrit = fCrit.transform.position;
            posCrit.y = posRec.y;
            fCrit.transform.position = posCrit;
        }
        // --------------------------------------------------------------------------------------------------------

        textoPuntaje.text = $"Puntaje: {reporte.puntaje}/100";
        textoNotaLetra.text = ObtenerNotaLetra(reporte.puntaje);
        textoResumen.text = $"<size=150%>Resumen:\n{reporte.resumen}</size>";
        textoEstadoEmocional.text = $"<size=150%>Estado Emocional:\n{reporte.estado_emocional}</size>";

        string recs = "<size=150%>Recomendaciones:\n";
        if (reporte.recomendaciones != null)
        {
            foreach (var r in reporte.recomendaciones)
            {
                recs += $"- {r}\n";
            }
        }
        recs += "</size>";
        textoRecomendaciones.text = recs;

        if (contenedorMomentosCriticos != null && plantillaMomentoCritico != null)
        {
            // Destruir items anteriores (excepto la plantilla y el titulo)
            foreach (Transform child in contenedorMomentosCriticos)
            {
                if (child.gameObject != plantillaMomentoCritico && child.name.StartsWith("Critico_"))
                {
                    Destroy(child.gameObject);
                }
            }

            plantillaMomentoCritico.SetActive(false);

            if (reporte.momentos_criticos != null && reporte.momentos_criticos.Count > 0)
            {
                for (int i = 0; i < reporte.momentos_criticos.Count; i++)
                {
                    var mc = reporte.momentos_criticos[i];
                    GameObject nuevoInst = Instantiate(plantillaMomentoCritico, contenedorMomentosCriticos);
                    nuevoInst.name = $"Critico_Instanciado_{i}";
                    nuevoInst.SetActive(true);

                    // Buscar textos en los hijos. Se asume orden: Pregunta, Observacion
                    TextMeshProUGUI[] textos = nuevoInst.GetComponentsInChildren<TextMeshProUGUI>();
                    if (textos.Length >= 2)
                    {
                        textos[0].text = $"<size=130%>{mc.pregunta}</size>";
                        textos[1].text = $"<size=150%>! {mc.observacion}</size>";
                    }
                }
            }
            else
            {
                // Si no hay momentos críticos, mostramos un mensaje positivo por defecto
                GameObject nuevoInst = Instantiate(plantillaMomentoCritico, contenedorMomentosCriticos);
                nuevoInst.name = "Critico_Vacio";
                nuevoInst.SetActive(true);

                TextMeshProUGUI[] textos = nuevoInst.GetComponentsInChildren<TextMeshProUGUI>();
                if (textos.Length >= 2)
                {
                    textos[0].text = "<size=130%>¡Excelente desempeño!</size>";
                    textos[1].text = "<size=150%>No se detectaron momentos de alta ansiedad o nerviosismo durante la entrevista.</size>";
                    textos[1].color = new Color(0.2f, 0.8f, 0.2f); // Un toque verde amigable
                }
            }
        }
        else
        {
            string mcs = "<size=150%>Momentos Críticos:\n";
            if (reporte.momentos_criticos != null)
            {
                foreach (var mc in reporte.momentos_criticos)
                {
                    mcs += $"Q: {mc.pregunta}\nObs: {mc.observacion}\n\n";
                }
            }
            mcs += "</size>";
            if (textoMomentosCriticos != null)
            {
                textoMomentosCriticos.text = mcs;
            }
        }
    }

    private string ObtenerNotaLetra(float puntaje)
    {
        if (puntaje >= 90) return "A";
        if (puntaje >= 80) return "B";
        if (puntaje >= 70) return "C";
        if (puntaje >= 60) return "D";
        return "F";
    }
}
