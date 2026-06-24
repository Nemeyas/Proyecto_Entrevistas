using UnityEngine;
using System.Collections;

/// <summary>
/// Controla las animaciones del entrevistador 3D.
/// Reacciona a las emociones del candidato y activa animación de habla.
/// Asignar este script al GameObject del modelo 3D del entrevistador.
/// 
/// Las animaciones NO-idle se reproducen una sola vez y luego el entrevistador
/// vuelve automáticamente al estado Idle (mirando la pantalla).
/// </summary>
public class EntrevistadorAnimator : MonoBehaviour
{
    private Animator animator;
    private Coroutine corutinaRetornoIdle;

    // Nombre del estado Idle en el Animator Controller (Base Layer)
    private const string ESTADO_IDLE = "Idle";

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
            ProgramarRetornoAIdle();
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
                ProgramarRetornoAIdle();
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
        
        if (anim == "talking")
        {
            animator.SetTrigger("Hablar");
            Debug.Log("[Entrevistador] Animacion: Hablando");
            ProgramarRetornoAIdle();
        }
        else if (anim == "laughing" || anim == "clap")
        {
            animator.SetTrigger("Positivo");
            Debug.Log("[Entrevistador] Animacion: Riendo/Positivo");
            ProgramarRetornoAIdle();
        }
        else if (anim == "idle")
        {
            VolverAIdleInmediato();
            Debug.Log("[Entrevistador] Animacion: Idle");
        }
        else
        {
            // Fallback a hablar
            animator.SetTrigger("Hablar");
            ProgramarRetornoAIdle();
        }
    }

    // =========================================================================
    // Lógica de retorno automático a Idle
    // =========================================================================

    /// <summary>
    /// Inicia la coroutine que espera a que la animación actual termine
    /// una reproducción completa y luego fuerza la transición a Idle.
    /// Si ya hay una coroutine en curso, la cancela para evitar conflictos.
    /// </summary>
    private void ProgramarRetornoAIdle()
    {
        if (corutinaRetornoIdle != null)
        {
            StopCoroutine(corutinaRetornoIdle);
        }
        corutinaRetornoIdle = StartCoroutine(EsperarYVolverAIdle());
    }

    /// <summary>
    /// Coroutine que:
    /// 1. Espera un frame para que el trigger sea consumido por el Animator.
    /// 2. Espera a que el Animator entre en un estado que NO sea Idle.
    /// 3. Espera a que la animación termine un ciclo completo (normalizedTime >= 1).
    /// 4. Hace CrossFade suave de vuelta al estado Idle.
    /// </summary>
    private IEnumerator EsperarYVolverAIdle()
    {
        // Dar un par de frames para que el Animator procese el trigger
        yield return null;
        yield return null;

        AnimatorStateInfo estadoActual = animator.GetCurrentAnimatorStateInfo(0);

        // Esperar a que realmente entre en un estado diferente a Idle
        float tiempoEspera = 0f;
        while (estadoActual.IsName(ESTADO_IDLE) && tiempoEspera < 2f)
        {
            yield return null;
            tiempoEspera += Time.deltaTime;
            estadoActual = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Si después de 2 segundos sigue en Idle, no hay nada que hacer
        if (estadoActual.IsName(ESTADO_IDLE))
        {
            corutinaRetornoIdle = null;
            yield break;
        }

        // Ahora esperar a que la animación termine un ciclo completo
        // normalizedTime >= 1.0 significa que completó al menos una reproducción
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
        {
            yield return null;
        }

        // Transición suave de vuelta a Idle
        animator.CrossFade(ESTADO_IDLE, 0.25f);
        Debug.Log("[Entrevistador] Retorno automatico a Idle");

        corutinaRetornoIdle = null;
    }

    /// <summary>
    /// Fuerza la vuelta a Idle inmediatamente (con transición suave).
    /// Útil cuando se solicita explícitamente "idle".
    /// </summary>
    private void VolverAIdleInmediato()
    {
        if (corutinaRetornoIdle != null)
        {
            StopCoroutine(corutinaRetornoIdle);
            corutinaRetornoIdle = null;
        }

        if (animator != null)
        {
            animator.CrossFade(ESTADO_IDLE, 0.25f);
        }
    }
}
