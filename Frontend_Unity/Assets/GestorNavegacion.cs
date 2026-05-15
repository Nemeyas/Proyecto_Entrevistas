using UnityEngine;
using UnityEngine.UI;

public class GestorNavegacion : MonoBehaviour
{
    public static GestorNavegacion Instancia { get; private set; }

    public GameObject panelMenu;
    public GameObject panelEntrevista;
    public GameObject panelReporte;
    public GameObject panelHistorial;
    public GameObject entorno3D;

    public int idSimulacionActiva = 0;
    public string nombrePostulanteActivo = "";
    public string rutPostulanteActivo = "";

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        MostrarPanel(panelMenu);
    }

    public void MostrarPanel(GameObject panelAMostrar)
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelEntrevista != null) panelEntrevista.SetActive(false);
        if (panelReporte != null) panelReporte.SetActive(false);
        if (panelHistorial != null) panelHistorial.SetActive(false);

        if (panelAMostrar != null)
        {
            panelAMostrar.SetActive(true);
        }

        if (entorno3D != null)
        {
            entorno3D.SetActive(panelAMostrar == panelEntrevista);
        }
    }

    public void MostrarMenu()
    {
        MostrarPanel(panelMenu);
    }

    public void MostrarEntrevista()
    {
        MostrarPanel(panelEntrevista);
        WebcamSender ws = FindObjectOfType<WebcamSender>();
        if (ws != null && !ws.entrevistaIniciada)
        {
            ws.IniciarEntrevista();
        }
    }

    public void MostrarReporte()
    {
        MostrarPanel(panelReporte);
    }

    public void MostrarHistorial()
    {
        MostrarPanel(panelHistorial);
    }
}
