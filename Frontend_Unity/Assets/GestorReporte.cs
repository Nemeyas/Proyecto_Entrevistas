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

        textoPuntaje.text = $"Puntaje: {reporte.puntaje}/100";
        textoNotaLetra.text = ObtenerNotaLetra(reporte.puntaje);
        textoResumen.text = "Resumen:\n" + reporte.resumen;
        textoEstadoEmocional.text = "Estado Emocional:\n" + reporte.estado_emocional;

        string recs = "Recomendaciones:\n";
        if (reporte.recomendaciones != null)
        {
            foreach (var r in reporte.recomendaciones)
            {
                recs += $"- {r}\n";
            }
        }
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
                        textos[0].text = mc.pregunta;
                        textos[1].text = "! " + mc.observacion;
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
                    textos[0].text = "¡Excelente desempeño!";
                    textos[1].text = "No se detectaron momentos de alta ansiedad o nerviosismo durante la entrevista.";
                    textos[1].color = new Color(0.2f, 0.8f, 0.2f); // Un toque verde amigable
                }
            }
        }
        else
        {
            string mcs = "Momentos Críticos:\n";
            if (reporte.momentos_criticos != null)
            {
                foreach (var mc in reporte.momentos_criticos)
                {
                    mcs += $"Q: {mc.pregunta}\nObs: {mc.observacion}\n\n";
                }
            }
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
