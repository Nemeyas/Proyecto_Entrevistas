using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class InicioEntrevistaResponse
{
    public string status;
    public int id_simulacion;
    public string mensaje;
}

public class GestorMenuPrincipal : MonoBehaviour
{
    public TMP_InputField inputNombre;
    public TMP_InputField inputRUT;
    public Button btnModoPasivo;
    public Button btnModoAgresivo;
    public Button btnVerHistorial;
    public TextMeshProUGUI textoError;

    private string urlIniciarEntrevista = "http://localhost:8000/iniciar_entrevista";

    void Start()
    {
        btnModoPasivo.onClick.AddListener(() => IniciarEntrevista("pasivo"));
        btnModoAgresivo.onClick.AddListener(() => IniciarEntrevista("agresivo"));
        btnVerHistorial.onClick.AddListener(VerHistorial);
    }

    void IniciarEntrevista(string dificultad)
    {
        string nombre = inputNombre.text.Trim();
        string rut = inputRUT.text.Trim();

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(rut))
        {
            if (textoError != null) textoError.text = "Por favor ingrese Nombre y RUT.";
            return;
        }

        if (textoError != null) textoError.text = "Iniciando...";
        StartCoroutine(EnviarDatosInicio(rut, nombre, dificultad));
    }

    IEnumerator EnviarDatosInicio(string rut, string nombre, string dificultad)
    {
        WWWForm form = new WWWForm();
        form.AddField("id_postulante", rut);
        form.AddField("nombre_postulante", nombre);
        form.AddField("dificultad", dificultad);

        using (UnityWebRequest www = UnityWebRequest.Post(urlIniciarEntrevista, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                InicioEntrevistaResponse response = JsonUtility.FromJson<InicioEntrevistaResponse>(jsonResponse);

                if (response.status == "exito")
                {
                    GestorNavegacion.Instancia.idSimulacionActiva = response.id_simulacion;
                    GestorNavegacion.Instancia.nombrePostulanteActivo = nombre;
                    GestorNavegacion.Instancia.rutPostulanteActivo = rut;
                    GestorNavegacion.Instancia.dificultadActiva = dificultad;

                    if (textoError != null) textoError.text = "";
                    GestorNavegacion.Instancia.MostrarEntrevista();
                }
                else
                {
                    if (textoError != null) textoError.text = "Error del servidor: " + response.mensaje;
                }
            }
            else
            {
                if (textoError != null) textoError.text = "Error de conexión.";
            }
        }
    }

    void VerHistorial()
    {
        GestorNavegacion.Instancia.MostrarHistorial();
    }
}
