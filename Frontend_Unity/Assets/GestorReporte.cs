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

        string mcs = "Momentos Críticos:\n";
        if (reporte.momentos_criticos != null)
        {
            foreach (var mc in reporte.momentos_criticos)
            {
                mcs += $"Q: {mc.pregunta}\nObs: {mc.observacion}\n\n";
            }
        }
        textoMomentosCriticos.text = mcs;
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
