using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necesario para el ScrollRect
using System.Collections;

public class GestorChat : MonoBehaviour
{
    [Header("Pantalla Principal")]
    public TextMeshProUGUI txtUltimoMensaje; 
    public TextMeshProUGUI txtEmocionVisible;

    [Header("Ventana de Historial")]
    public GameObject objetoPanelLog;
    public TextMeshProUGUI txtMuroCompleto;
    public ScrollRect scrollDelLog; // <-- ¡NUEVO! Controlará la rueda del ratón

    void Awake()
    {
        LimpiarChat();
    }

    public void LimpiarChat()
    {
        if (txtMuroCompleto != null) txtMuroCompleto.text = "";
        if (txtUltimoMensaje != null) txtUltimoMensaje.text = "";
    }

    // NUEVO: Función para inyectar el saludo inicial directamente al muro
    public void AgregarMensajeLog(string nombre, string mensaje, string colorHex)
    {
        if (txtMuroCompleto != null)
        {
            txtMuroCompleto.text += $"<color={colorHex}><b>{nombre}:</b></color> {mensaje}\n\n";
            StartCoroutine(BajarScroll());
        }
    }

    // Se llama cada vez que tú y Gemini hablan
    public void ActualizarConversacion(string usuario, string ia)
    {
        if (txtMuroCompleto != null)
        {
            txtMuroCompleto.text += $"<color=#00FF00><b>Tú:</b></color> {usuario}\n\n";
            txtMuroCompleto.text += $"<color=#FFA500><b>Entrevistador:</b></color> {ia}\n\n";
            
            StartCoroutine(BajarScroll());
        }
    }

    IEnumerator BajarScroll()
    {
        // Unity necesita 1 frame para calcular cuánto creció el texto antes de bajar
        yield return new WaitForEndOfFrame();
        
        // Empujamos la barra al fondo (0 = abajo, 1 = arriba)
        if (scrollDelLog != null)
        {
            scrollDelLog.verticalNormalizedPosition = 0f;
        }
    }
}