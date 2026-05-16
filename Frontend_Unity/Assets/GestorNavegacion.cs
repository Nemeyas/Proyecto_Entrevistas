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

    // --- Modelos del entrevistador ---
    public GameObject modeloPasivo;
    public GameObject modeloAgresivo;

    public string dificultadActiva = "pasivo";
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
            bool mostrarEntorno = (panelAMostrar == panelEntrevista);
            entorno3D.SetActive(mostrarEntorno);

            if (mostrarEntorno)
            {
                ActivarModeloSegunDificultad();
            }
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

    /// <summary>
    /// Activa el modelo 3D correspondiente a la dificultad seleccionada
    /// y actualiza la referencia del animator en WebcamSender.
    /// </summary>
    void ActivarModeloSegunDificultad()
    {
        bool esPasivo = (dificultadActiva == "pasivo");

        if (modeloPasivo != null) modeloPasivo.SetActive(esPasivo);
        if (modeloAgresivo != null) modeloAgresivo.SetActive(!esPasivo);

        // Actualizar la referencia del EntrevistadorAnimator en WebcamSender
        WebcamSender ws = FindObjectOfType<WebcamSender>();
        if (ws != null)
        {
            GameObject modeloActivo = esPasivo ? modeloPasivo : modeloAgresivo;
            if (modeloActivo != null)
            {
                ws.miEntrevistadorAnimator = modeloActivo.GetComponent<EntrevistadorAnimator>();
            }
        }

        Debug.Log($"[GestorNavegacion] Modelo activado: {(esPasivo ? "Pasivo" : "Agresivo")}");
    }
}
