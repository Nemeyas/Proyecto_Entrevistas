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
    public string animacion_entrevistador;
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
    
    // --- Variables de Estado y UI ---
    private bool procesandoPeticion = false;
    public GameObject botonFinalizar;
    
    // --- Selección de Dispositivos ---
    public TMP_Dropdown dropdownCamara;
    public TMP_Dropdown dropdownMicrofono;
    private string nombreCamaraSeleccionada = "";
    private string nombreMicrofonoSeleccionado = "";

    void Start()
    {
        // El botón finalizar arranca apagado
        if (botonFinalizar != null) botonFinalizar.SetActive(false);
        
        PoblarDispositivos();
    }

    void PoblarDispositivos()
    {
        if (dropdownCamara != null)
        {
            dropdownCamara.ClearOptions();
            System.Collections.Generic.List<string> opcionesCamara = new System.Collections.Generic.List<string>();
            WebCamDevice[] dispositivosCamara = WebCamTexture.devices;
            int indiceCamaraDefecto = 0;

            for (int i = 0; i < dispositivosCamara.Length; i++)
            {
                opcionesCamara.Add(dispositivosCamara[i].name);
                if (dispositivosCamara[i].name.ToLower().Contains("droid"))
                {
                    indiceCamaraDefecto = i;
                }
            }

            dropdownCamara.AddOptions(opcionesCamara);
            if (opcionesCamara.Count > 0)
            {
                dropdownCamara.value = indiceCamaraDefecto;
                nombreCamaraSeleccionada = opcionesCamara[indiceCamaraDefecto];
            }

            dropdownCamara.onValueChanged.AddListener(delegate {
                nombreCamaraSeleccionada = opcionesCamara[dropdownCamara.value];
                CambiarCamara();
            });
        }

        if (dropdownMicrofono != null)
        {
            dropdownMicrofono.ClearOptions();
            System.Collections.Generic.List<string> opcionesMicrofono = new System.Collections.Generic.List<string>();
            string[] dispositivosMic = Microphone.devices;

            for (int i = 0; i < dispositivosMic.Length; i++)
            {
                opcionesMicrofono.Add(dispositivosMic[i]);
            }

            dropdownMicrofono.AddOptions(opcionesMicrofono);
            if (opcionesMicrofono.Count > 0)
            {
                dropdownMicrofono.value = 0;
                nombreMicrofonoSeleccionado = opcionesMicrofono[0];
            }

            dropdownMicrofono.onValueChanged.AddListener(delegate {
                nombreMicrofonoSeleccionado = opcionesMicrofono[dropdownMicrofono.value];
            });
        }
    }

    public void CambiarCamara()
    {
        if (entrevistaIniciada)
        {
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }
            if (!string.IsNullOrEmpty(nombreCamaraSeleccionada))
            {
                webcamTexture = new WebCamTexture(nombreCamaraSeleccionada);
            }
            else
            {
                webcamTexture = new WebCamTexture();
            }
            if (pantallaCamara != null) pantallaCamara.texture = webcamTexture;
            webcamTexture.Play();
        }
    }


    public void IniciarEntrevista()
    {
        // Si hay cámara seleccionada por el dropdown usarla, si no autodetectar
        string camaraElegida = nombreCamaraSeleccionada;
        
        if (string.IsNullOrEmpty(camaraElegida))
        {
            WebCamDevice[] dispositivos = WebCamTexture.devices;
            for (int i = 0; i < dispositivos.Length; i++)
            {
                if (dispositivos[i].name.ToLower().Contains("droid"))
                {
                    camaraElegida = dispositivos[i].name;
                }
            }
        }

        if (!string.IsNullOrEmpty(camaraElegida))
        {
            Debug.Log($"[Webcam] >>> Usando cámara: {camaraElegida}");
            webcamTexture = new WebCamTexture(camaraElegida);
        }
        else
        {
            Debug.Log("[Webcam] >>> Cámara no especificada, usando por defecto.");
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
        if (procesandoPeticion) return; // BLOQUEO DE UI

        if (!estaGrabando)
        {
            estaGrabando = true;
            textoDelBoton.text = "🔴 Grabando...";
            textoDelBoton.color = Color.red;
            
            string micAUsar = string.IsNullOrEmpty(nombreMicrofonoSeleccionado) ? null : nombreMicrofonoSeleccionado;
            clipGrabado = Microphone.Start(micAUsar, false, 15, 44100);
        }
        else
        {
            estaGrabando = false;
            
            // IMPORTANTE: Obtener la posición REAL del micrófono ANTES de detenerlo.
            // Sin esto, el AudioClip tiene 15 segundos de buffer y los samples
            // después de la grabación real son basura/ruido que Google Speech
            // interpreta como repeticiones de palabras (ej: "hola hola hola...").
            string micAUsar = string.IsNullOrEmpty(nombreMicrofonoSeleccionado) ? null : nombreMicrofonoSeleccionado;
            int posicionReal = Microphone.GetPosition(micAUsar);
            Microphone.End(micAUsar);
            
            // Recortar el clip para enviar SOLO el audio que realmente se grabó
            if (posicionReal > 0)
            {
                float[] datosReales = new float[posicionReal * clipGrabado.channels];
                clipGrabado.GetData(datosReales, 0);
                AudioClip clipRecortado = AudioClip.Create("grabacion_recortada", posicionReal, clipGrabado.channels, clipGrabado.frequency, false);
                clipRecortado.SetData(datosReales, 0);
                clipGrabado = clipRecortado;
            }
            
            textoDelBoton.text = "⏳ Enviando...";
            textoDelBoton.color = Color.yellow;
            
            StartCoroutine(EnviarAudioAlServidor());
        }
    }

    IEnumerator EnviarAudioAlServidor()
    {
        procesandoPeticion = true; // BLOQUEO DE UI
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
                        if (!string.IsNullOrEmpty(respuestaAudio.animacion_entrevistador))
                        {
                            miEntrevistadorAnimator.EjecutarAnimacionIA(respuestaAudio.animacion_entrevistador);
                        }
                        else
                        {
                            miEntrevistadorAnimator.ActivarHabla();
                        }
                    }

                    // Si el backend dice que la entrevista terminó, finalizar automáticamente o mostrar botón
                    if (respuestaAudio.entrevista_terminada)
                    {
                        if (botonFinalizar != null)
                        {
                            botonFinalizar.SetActive(true); // Dar la señal visual al usuario
                        }
                        else
                        {
                            yield return new WaitForSeconds(2f); // Dar tiempo a leer la despedida
                            FinalizarEntrevista();
                        }
                        procesandoPeticion = false; // DESBLOQUEO DE UI
                        yield break;
                    }
                }
            }

            textoDelBoton.text = "Hablar";
            textoDelBoton.color = Color.black;
            procesandoPeticion = false; // DESBLOQUEO DE UI
        }
    }

    public void FinalizarEntrevista()
    {
        if (procesandoPeticion) return; // Evitar spam del botón
        StartCoroutine(EnviarPeticionFinalizar());
    }

    IEnumerator EnviarPeticionFinalizar()
    {
        procesandoPeticion = true; // BLOQUEO DE UI
        
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

                    GestorNavegacion.Instancia.MostrarReporte();
                    GestorReporte gestorReporte = FindObjectOfType<GestorReporte>();
                    if (gestorReporte != null)
                    {
                        gestorReporte.MostrarDatosReporte(response.reporte);
                    }
                }
            }

            if (textoDelBoton != null)
            {
                textoDelBoton.text = "Hablar";
                textoDelBoton.color = Color.black;
            }
            
            procesandoPeticion = false; // DESBLOQUEO DE UI
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