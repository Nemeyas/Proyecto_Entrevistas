using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class RespuestaPython
{
    public string status;
    public string emocion;
    public string respuesta_ia;
}

[System.Serializable]
public class RespuestaServidorAudio
{
    public string status;
    public string transcripcion;
    public string respuesta_ia;
    public bool entrevista_terminada;
}

public class WebcamSender : MonoBehaviour
{
    WebCamTexture webcamTexture;
    public string serverURL_Emocion = "http://localhost:8000/analizar_emocion";
    public string serverURL_Audio = "http://localhost:8000/procesar_audio";
    public string serverURL_Finalizar = "http://localhost:8000/finalizar_entrevista";
    public RawImage pantallaCamara;
    public TextMeshProUGUI textoEmocion; 
    
    public TextMeshProUGUI textoDelBoton; 
    private AudioClip clipGrabado;
    private bool estaGrabando = false;
    
    public GestorChat miGestorDeChat;
    public EntrevistadorAnimator miEntrevistadorAnimator;
    public bool entrevistaIniciada = false;

    void Start()
    {
        // Si no hay menú principal en la escena, creamos uno automáticamente
        if (GestorNavegacion.Instancia == null || GestorNavegacion.Instancia.panelMenu == null)
        {
            if (FindObjectOfType<MenuAutoBuilder>() == null)
            {
                gameObject.AddComponent<MenuAutoBuilder>();
            }
        }
    }

    public void IniciarEntrevista()
    {
        // --- Detección inteligente de cámara ---
        WebCamDevice[] dispositivos = WebCamTexture.devices;
        string camaraElegida = "";

        Debug.Log($"[Webcam] Se encontraron {dispositivos.Length} cámara(s):");
        for (int i = 0; i < dispositivos.Length; i++)
        {
            Debug.Log($"  [{i}] {dispositivos[i].name}");
            // Buscar DroidCam (o cualquier cámara virtual con "Droid" en el nombre)
            if (dispositivos[i].name.ToLower().Contains("droid"))
            {
                camaraElegida = dispositivos[i].name;
            }
        }

        // Si encontró DroidCam, usarla. Si no, usar la cámara por defecto.
        if (!string.IsNullOrEmpty(camaraElegida))
        {
            Debug.Log($"[Webcam] >>> Usando DroidCam: {camaraElegida}");
            webcamTexture = new WebCamTexture(camaraElegida);
        }
        else
        {
            Debug.Log("[Webcam] >>> DroidCam no encontrada, usando cámara por defecto.");
            webcamTexture = new WebCamTexture();
        }

        if (pantallaCamara != null) pantallaCamara.texture = webcamTexture;
        webcamTexture.Play();

        // --- ¡AQUÍ ESTÁ LA MAGIA DEL SALUDO INICIAL! ---
        if (miGestorDeChat != null)
        {
            miGestorDeChat.AgregarMensajeLog("Entrevistador", "Hola, bienvenido a la entrevista. Háblame de un desafío que hayas superado.", "#FFA500");
        }
        // -----------------------------------------------

        entrevistaIniciada = true;
        StartCoroutine(EnviarFotoRutinariamente());
    }

    public void DetenerCamara()
    {
        entrevistaIniciada = false;
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
        StopAllCoroutines();
    }

    IEnumerator EnviarFotoRutinariamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(3.0f);

            if (webcamTexture.isPlaying)
            {
                Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
                photo.SetPixels(webcamTexture.GetPixels());
                photo.Apply();
                byte[] bytes = photo.EncodeToJPG();

                WWWForm form = new WWWForm();
                form.AddBinaryData("file", bytes, "captura.jpg", "image/jpeg");

                using (UnityWebRequest www = UnityWebRequest.Post(serverURL_Emocion, form))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        string jsonString = www.downloadHandler.text;
                        RespuestaPython respuesta = JsonUtility.FromJson<RespuestaPython>(jsonString);
                        
                        if (textoEmocion != null) 
                        {
                            string em = respuesta.emocion.ToLower();
                            string emoji = em == "happy" ? "😊" : em == "sad" ? "😢" : em == "angry" ? "😠" : em == "surprise" ? "😲" : em == "fear" ? "😨" : em == "disgust" ? "🤢" : "😐";
                            Color color = em == "happy" ? Color.green : em == "angry" ? Color.red : Color.white;
                            
                            textoEmocion.text = $"Emoción: {respuesta.emocion.ToUpper()} {emoji}";
                            textoEmocion.color = color;
                        }

                        // Hacer que el entrevistador 3D reaccione a la emoción
                        if (miEntrevistadorAnimator != null)
                        {
                            miEntrevistadorAnimator.ReaccionarAEmocion(respuesta.emocion);
                        }
                    }
                }
                Destroy(photo);
            }
        }
    }

    public void AlternarGrabacion()
    {
        if (!estaGrabando)
        {
            estaGrabando = true;
            textoDelBoton.text = "🔴 Grabando...";
            textoDelBoton.color = Color.red;
            clipGrabado = Microphone.Start(null, false, 15, 44100);
        }
        else
        {
            estaGrabando = false;
            Microphone.End(null);
            textoDelBoton.text = "⏳ Enviando...";
            textoDelBoton.color = Color.yellow;
            
            StartCoroutine(EnviarAudioAlServidor());
        }
    }

    IEnumerator EnviarAudioAlServidor()
    {
        byte[] wavBytes = ConvertirAWav(clipGrabado);
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavBytes, "respuesta.wav", "audio/wav");
        if (GestorNavegacion.Instancia != null)
        {
            form.AddField("id_simulacion", GestorNavegacion.Instancia.idSimulacionActiva);
        }

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL_Audio, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonString = www.downloadHandler.text;
                RespuestaServidorAudio respuestaAudio = JsonUtility.FromJson<RespuestaServidorAudio>(jsonString);
                
                if (respuestaAudio.status == "exito")
                {
                    if (miGestorDeChat != null)
                    {
                        miGestorDeChat.ActualizarConversacion(respuestaAudio.transcripcion, respuestaAudio.respuesta_ia);
                    }

                    if (miEntrevistadorAnimator != null)
                    {
                        miEntrevistadorAnimator.ActivarHabla();
                    }

                    // Si el backend dice que la entrevista terminó, finalizar automáticamente
                    if (respuestaAudio.entrevista_terminada)
                    {
                        yield return new WaitForSeconds(2f); // Dar tiempo a leer la despedida
                        FinalizarEntrevista();
                        yield break;
                    }
                }
            }

            textoDelBoton.text = "Hablar";
            textoDelBoton.color = Color.black;
        }
    }

    public void FinalizarEntrevista()
    {
        StartCoroutine(EnviarPeticionFinalizar());
    }

    IEnumerator EnviarPeticionFinalizar()
    {
        if (textoDelBoton != null)
        {
            textoDelBoton.text = "Generando Reporte...";
            textoDelBoton.color = Color.yellow;
        }

        WWWForm form = new WWWForm();
        if (GestorNavegacion.Instancia != null)
        {
            form.AddField("id_simulacion", GestorNavegacion.Instancia.idSimulacionActiva);
        }

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL_Finalizar, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonString = www.downloadHandler.text;
                ResponseReporteBackend response = JsonUtility.FromJson<ResponseReporteBackend>(jsonString);

                if (response.status == "exito" && response.reporte != null)
                {
                    DetenerCamara();

                    // Usar PanelUIBuilder para mostrar el reporte programáticamente
                    PanelUIBuilder builder = FindObjectOfType<PanelUIBuilder>();
                    if (builder == null) builder = gameObject.AddComponent<PanelUIBuilder>();
                    builder.MostrarReporte(response.reporte);
                }
            }

            if (textoDelBoton != null)
            {
                textoDelBoton.text = "Hablar";
                textoDelBoton.color = Color.black;
            }
        }
    }

    byte[] ConvertirAWav(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;
        float[] data = new float[samples * channels];
        clip.GetData(data, 0);

        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + samples * channels * 2);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(hz);
        writer.Write(hz * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(samples * channels * 2);

        foreach (float sample in data)
        {
            writer.Write((short)(sample * short.MaxValue));
        }
        return stream.ToArray();
    }
}