using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class HistorialItem
{
    public int IDSimulacion;
    public string NombrePostulante;
    public string TiempoInicio;
    public string Dificultad;
    public float PuntajeGlobal;
}

[System.Serializable]
public class HistorialResponse
{
    public string status;
    public List<HistorialItem> historial;
    public string mensaje;
}

[System.Serializable]
public class ReporteDetailResponse
{
    public string status;
    public ReporteDataContainer reporte;
}

[System.Serializable]
public class ReporteDataContainer
{
    public int IDSimulacion;
    public string NombrePostulante;
    public string Dificultad;
    public float PuntajeGlobal;
    public string Resumen;
    public DatosReporte Resumen_JSON;
}

public class GestorHistorial : MonoBehaviour
{
    public GameObject prefabTarjeta;
    public Transform contenedorTarjetas;
    public Button btnVolverMenu;
    public TextMeshProUGUI textoCargando;

    private string urlHistorial = "http://localhost:8000/historial_reportes";
    private string urlReporte = "http://localhost:8000/reporte/";

    void Start()
    {
        if (btnVolverMenu != null)
        {
            btnVolverMenu.onClick.AddListener(() => {
                GestorNavegacion.Instancia.MostrarMenu();
            });
        }
    }

    void OnEnable()
    {
        CargarHistorial();
    }

    void CargarHistorial()
    {
        foreach (Transform child in contenedorTarjetas)
        {
            Destroy(child.gameObject);
        }
        if (textoCargando != null) textoCargando.gameObject.SetActive(true);
        
        StartCoroutine(PeticionHistorial());
    }

    IEnumerator PeticionHistorial()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(urlHistorial))
        {
            yield return www.SendWebRequest();

            if (textoCargando != null) textoCargando.gameObject.SetActive(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                // UnityEngine.JsonUtility needs a wrapper class for top-level array, which we have (HistorialResponse)
                HistorialResponse response = JsonUtility.FromJson<HistorialResponse>(jsonResponse);

                if (response.status == "exito" && response.historial != null)
                {
                    foreach (var item in response.historial)
                    {
                        GameObject tarjeta = Instantiate(prefabTarjeta, contenedorTarjetas);
                        
                        TextMeshProUGUI[] textos = tarjeta.GetComponentsInChildren<TextMeshProUGUI>();
                        if (textos.Length >= 4)
                        {
                            textos[0].text = $"ID: {item.IDSimulacion} - {item.NombrePostulante}";
                            textos[1].text = $"Modo: {item.Dificultad}";
                            textos[2].text = $"Fecha: {item.TiempoInicio}";
                            textos[3].text = $"Puntaje: {item.PuntajeGlobal}/100";
                        }

                        Button btn = tarjeta.GetComponentInChildren<Button>();
                        if (btn != null)
                        {
                            int idSimulacion = item.IDSimulacion;
                            btn.onClick.AddListener(() => VerDetalleReporte(idSimulacion));
                        }
                    }
                }
            }
        }
    }

    void VerDetalleReporte(int idSimulacion)
    {
        StartCoroutine(PeticionDetalleReporte(idSimulacion));
    }

    IEnumerator PeticionDetalleReporte(int idSimulacion)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(urlReporte + idSimulacion))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                ReporteDetailResponse response = JsonUtility.FromJson<ReporteDetailResponse>(jsonResponse);

                if (response.status == "exito" && response.reporte != null && response.reporte.Resumen_JSON != null)
                {
                    GestorNavegacion.Instancia.MostrarReporte();
                    GestorReporte gestorReporte = FindObjectOfType<GestorReporte>();
                    if (gestorReporte != null)
                    {
                        gestorReporte.MostrarDatosReporte(response.reporte.Resumen_JSON);
                    }
                }
            }
        }
    }
}
