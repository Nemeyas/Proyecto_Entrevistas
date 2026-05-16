using UnityEngine;

/// <summary>
/// Controla las animaciones del entrevistador 3D.
/// Reacciona a las emociones del candidato y activa animación de habla.
/// Asignar este script al GameObject del modelo 3D del entrevistador.
/// </summary>
public class EntrevistadorAnimator : MonoBehaviour
{
    private Animator animator;

    // Emociones que se consideran "positivas" y "negativas"
    private readonly string[] emocionesPositivas = { "happy", "surprise" };
    private readonly string[] emocionesNegativas = { "angry", "sad", "fear", "disgust" };

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[EntrevistadorAnimator] No se encontro el componente Animator. Asegurate de asignarlo.");
        }
    }

    /// <summary>
    /// Activa la animación de "hablando" (cuando Gemini responde).
    /// Llamado desde WebcamSender cuando llega la respuesta del servidor.
    /// </summary>
    public void ActivarHabla()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hablar");
            Debug.Log("[Entrevistador] Animacion: Hablando");
        }
    }

    /// <summary>
    /// Recibe la emoción del candidato y reacciona con una animación.
    /// Llamado desde WebcamSender cuando llega el análisis de emoción.
    /// </summary>
    public void ReaccionarAEmocion(string emocion)
    {
        if (animator == null) return;

        string em = emocion.ToLower();

        // Revisar si es positiva
        foreach (string positiva in emocionesPositivas)
        {
            if (em == positiva)
            {
                animator.SetTrigger("Positivo");
                Debug.Log($"[Entrevistador] Reaccion positiva a: {emocion}");
                return;
            }
        }

        // Revisar si es negativa
        foreach (string negativa in emocionesNegativas)
        {
            if (em == negativa)
            {
                // animator.SetTrigger("Negativo"); // Trigger 'Negativo' does not exist in controller
                Debug.Log($"[Entrevistador] Reaccion negativa a: {emocion}");
                return;
            }
        }

        // Neutral: no hacer nada especial
        Debug.Log($"[Entrevistador] Emocion neutral: {emocion}");
    }

    /// <summary>
    /// Ejecuta una animación específica solicitada por la IA (ej. "idle", "talking", "laughing", "clap").
    /// </summary>
    public void EjecutarAnimacionIA(string animacion)
    {
        if (animator == null || string.IsNullOrEmpty(animacion)) return;

        string anim = animacion.ToLower();
        
        // Dependiendo de cómo estén configurados los triggers en el Animator, 
        // aquí los llamamos. Asumimos que existen triggers con estos nombres.
        if (anim == "talking")
        {
            animator.SetTrigger("Hablar");
            Debug.Log("[Entrevistador] Animacion: Hablando");
        }
        else if (anim == "laughing" || anim == "clap")
        {
            animator.SetTrigger("Positivo");
            Debug.Log("[Entrevistador] Animacion: Riendo/Positivo");
        }
        else if (anim == "idle")
        {
            // Do nothing, since transitions return to idle automatically.
            Debug.Log("[Entrevistador] Animacion: Idle");
        }
        else
        {
            // Fallback a hablar
            animator.SetTrigger("Hablar");
        }
    }
}
