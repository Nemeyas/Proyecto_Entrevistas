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

public class WebcamSender : MonoBehaviour
{
    // --- VARIABLES DE CÁMARA ---
    WebCamTexture webcamTexture;
    public string serverURL_Emocion = "http://localhost:8000/analizar_emocion";
    public string serverURL_Audio = "http://localhost:8000/procesar_audio"; // Nueva ruta para el LLM
    public RawImage pantallaCamara;
    public TextMeshProUGUI textoEmocion; 
    public TextMeshProUGUI textoEntrevistador;

    // --- VARIABLES DE AUDIO ---
    public TextMeshProUGUI textoDelBoton; // Para avisarte cuando está grabando
    private AudioClip clipGrabado;
    private bool estaGrabando = false;

    void Start()
    {
        // 1. Iniciar Cámara
        webcamTexture = new WebCamTexture();
        if (pantallaCamara != null) pantallaCamara.texture = webcamTexture;
        webcamTexture.Play();
        
        if (textoEntrevistador != null) 
            textoEntrevistador.text = "Hola, bienvenido a la entrevista. Háblame de un desafío que hayas superado.";

        StartCoroutine(EnviarFotoRutinariamente());
    }

    // ==========================================
    // SECCIÓN 1: FOTO Y EMOCIÓN (Lo que ya funciona)
    // ==========================================
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
                    }
                }
                Destroy(photo);
            }
        }
    }

    // ==========================================
    // SECCIÓN 2: GRABACIÓN DE MICRÓFONO (NUEVO)
    // ==========================================
    
    // Esta es la función que conectaremos al botón
    public void AlternarGrabacion()
    {
        if (!estaGrabando)
        {
            // Empezar a grabar (máximo 15 segundos)
            estaGrabando = true;
            textoDelBoton.text = "🔴 Grabando... (Click para detener)";
            textoDelBoton.color = Color.red;
            clipGrabado = Microphone.Start(null, false, 15, 44100);
        }
        else
        {
            // Detener y enviar
            estaGrabando = false;
            Microphone.End(null);
            textoDelBoton.text = "⏳ Enviando a IA...";
            textoDelBoton.color = Color.yellow;
            
            StartCoroutine(EnviarAudioAlServidor());
        }
    }

    IEnumerator EnviarAudioAlServidor()
    {
        // Convertimos el audio de Unity a un archivo .wav real
        byte[] wavBytes = ConvertirAWav(clipGrabado);
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavBytes, "respuesta.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL_Audio, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonString = www.downloadHandler.text;
                RespuestaPython respuesta = JsonUtility.FromJson<RespuestaPython>(jsonString);
                
                // Imprimimos en consola y actualizamos el texto del entrevistador en pantalla
                Debug.Log("Python respondió: " + respuesta.respuesta_ia);
                if (textoEntrevistador != null && respuesta.respuesta_ia != null)
                {
                    textoEntrevistador.text = respuesta.respuesta_ia;
                }
            }

            // Restaurar el botón
            textoDelBoton.text = "Hablar";
            textoDelBoton.color = Color.black;
        }
    }

    // ==========================================
    // FUNCIÓN DE AYUDA: CONVERTIR A WAV (No tocar)
    // ==========================================
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