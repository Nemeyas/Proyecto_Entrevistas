using UnityEngine;
using System.Collections;

/// <summary>
/// Controla las animaciones del entrevistador 3D.
/// El modelo permanece en Idle (respirando) el 80% del tiempo.
/// Cuando recibe texto/eventos, hace un gesto sutil y rápido,
/// y luego vuelve al Idle inmediatamente.
/// El entrevistador mantiene la vista hacia la persona la mayor parte del tiempo.
/// </summary>
public class EntrevistadorAnimator : MonoBehaviour
{
    private Animator animator;
    private Coroutine corutinaGesto;

    // Nombre del estado Idle en el Animator Controller (Base Layer)
    private const string ESTADO_IDLE = "Idle";

    // Configuración del gesto sutil
    [Header("Configuración de Gesto Sutil")]
    [Tooltip("Duración máxima del gesto antes de volver a Idle (en segundos)")]
    public float duracionGesto = 0.4f;

    [Tooltip("Velocidad de transición suave de vuelta a Idle")]
    public float velocidadTransicion = 0.15f;

    // Control de mirada hacia la persona (cámara)
    [Header("Seguimiento de Mirada")]
    [Tooltip("Transform de la cabeza del modelo (asignar en Inspector)")]
    public Transform huezoCabeza;

    [Tooltip("Objetivo de mirada (cámara principal si no se asigna)")]
    public Transform objetivoMirada;

    [Tooltip("Porcentaje del tiempo mirando al objetivo (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float porcentajeMirada = 0.8f;

    [Tooltip("Peso del seguimiento de mirada (0 = nada, 1 = total)")]
    [Range(0f, 1f)]
    public float pesoMirada = 0.6f;

    [Tooltip("Velocidad de suavizado de la mirada")]
    public float suavizadoMirada = 3f;

    private float pesoMiradaActual = 0f;
    private bool mirandoObjetivo = true;
    private float temporizadorMirada = 0f;
    private float duracionMiradaActual = 3f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[EntrevistadorAnimator] No se encontro el componente Animator. Asegurate de asignarlo.");
        }

        // Si no se asignó un objetivo de mirada, usar la cámara principal
        if (objetivoMirada == null && Camera.main != null)
        {
            objetivoMirada = Camera.main.transform;
        }
    }

    void Update()
    {
        ActualizarCicloMirada();
    }

    void LateUpdate()
    {
        AplicarMirada();
    }

    // =========================================================================
    // Lógica de mirada natural (80% mirando a la persona)
    // =========================================================================

    /// <summary>
    /// Alterna de forma natural entre mirar al objetivo y mirar ligeramente a otro lado.
    /// Simula un patrón de mirada humano donde mira ~80% del tiempo a la persona.
    /// </summary>
    private void ActualizarCicloMirada()
    {
        temporizadorMirada -= Time.deltaTime;

        if (temporizadorMirada <= 0f)
        {
            // Decidir si mirar al objetivo o no, respetando el porcentaje configurado
            mirandoObjetivo = Random.value < porcentajeMirada;

            // Duración aleatoria para que se sienta natural
            if (mirandoObjetivo)
            {
                duracionMiradaActual = Random.Range(2f, 5f); // Mira al candidato entre 2 y 5 segundos
            }
            else
            {
                duracionMiradaActual = Random.Range(0.5f, 1.5f); // Desvía la mirada brevemente
            }

            temporizadorMirada = duracionMiradaActual;
        }

        // Suavizar la transición del peso de mirada
        float pesoObjetivo = mirandoObjetivo ? pesoMirada : 0f;
        pesoMiradaActual = Mathf.Lerp(pesoMiradaActual, pesoObjetivo, Time.deltaTime * suavizadoMirada);
    }

    /// <summary>
    /// Aplica la rotación de la cabeza hacia el objetivo de mirada.
    /// Se ejecuta en LateUpdate para sobreescribir la animación del Animator.
    /// </summary>
    private void AplicarMirada()
    {
        if (huezoCabeza == null || objetivoMirada == null) return;
        if (pesoMiradaActual < 0.01f) return;

        // Calcular la dirección hacia el objetivo
        Vector3 direccionAlObjetivo = objetivoMirada.position - huezoCabeza.position;
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionAlObjetivo);

        // Mezclar la rotación de la animación con la mirada
        huezoCabeza.rotation = Quaternion.Slerp(huezoCabeza.rotation, rotacionObjetivo, pesoMiradaActual);
    }

    // =========================================================================
    // Interfaz pública — Gesto sutil al recibir eventos
    // =========================================================================

    /// <summary>
    /// Activa un gesto sutil cuando el entrevistador "habla" (cuando Gemini responde).
    /// El gesto es brevísimo: perturba ligeramente la animación y vuelve rápido a Idle.
    /// </summary>
    public void ActivarHabla()
    {
        RealizarGestoSutil();
        Debug.Log("[Entrevistador] Gesto sutil: recibió respuesta IA");
    }

    /// <summary>
    /// Recibe la emoción del candidato. En esta versión simplificada,
    /// solo hace un gesto sutil sin importar la emoción.
    /// El entrevistador se mantiene sereno y en Idle.
    /// </summary>
    public void ReaccionarAEmocion(string emocion)
    {
        // En la versión simplificada no reacciona visiblemente a emociones.
        // Solo logea para debug.
        Debug.Log($"[Entrevistador] Emocion detectada (sin reaccion visible): {emocion}");
    }

    /// <summary>
    /// Ejecuta una animación solicitada por la IA.
    /// En esta versión simplificada, todas las solicitudes resultan en un gesto sutil
    /// que rápidamente vuelve a Idle (respiración).
    /// </summary>
    public void EjecutarAnimacionIA(string animacion)
    {
        if (string.IsNullOrEmpty(animacion)) return;

        string anim = animacion.ToLower();

        if (anim == "idle")
        {
            // Si piden idle explícitamente, quedarse en idle sin gesto
            VolverAIdleInmediato();
            Debug.Log("[Entrevistador] Solicitado: Idle");
        }
        else
        {
            // Cualquier otra animación → gesto sutil y de vuelta a idle
            RealizarGestoSutil();
            Debug.Log($"[Entrevistador] Gesto sutil por solicitud: {animacion}");
        }
    }

    // =========================================================================
    // Gesto sutil — Perturbación breve que vuelve rápido a Idle
    // =========================================================================

    /// <summary>
    /// Realiza un gesto sutil: activa brevemente una variación de la animación
    /// y vuelve rápidamente al Idle (respiración).
    /// Esto crea un movimiento ligero que se siente natural sin romper la pose.
    /// </summary>
    private void RealizarGestoSutil()
    {
        if (animator == null) return;

        // Cancelar cualquier gesto previo en curso
        if (corutinaGesto != null)
        {
            StopCoroutine(corutinaGesto);
        }

        corutinaGesto = StartCoroutine(GestoSutilCoroutine());
    }

    /// <summary>
    /// Coroutine que simula un gesto sutil:
    /// 1. Cambia brevemente la velocidad de la animación Idle para crear un "micro-movimiento".
    /// 2. Después de un breve instante, restaura todo al estado normal.
    /// Esto genera un movimiento perceptible pero que no rompe la pose de respiración.
    /// </summary>
    private IEnumerator GestoSutilCoroutine()
    {
        // Opción 1: Usar variación de velocidad en el Idle para crear micro-gesto
        // Esto es más sutil que cambiar de estado y evita problemas de transición
        float velocidadOriginal = animator.speed;

        // Acelerar brevemente para crear un movimiento perceptible
        animator.speed = 1.8f;

        yield return new WaitForSeconds(duracionGesto);

        // Volver a velocidad normal suavemente
        float tiempoRestauracion = 0.3f;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoRestauracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            animator.speed = Mathf.Lerp(1.8f, velocidadOriginal, tiempoTranscurrido / tiempoRestauracion);
            yield return null;
        }

        animator.speed = velocidadOriginal;

        // Asegurarse de que estamos en Idle
        AnimatorStateInfo estadoActual = animator.GetCurrentAnimatorStateInfo(0);
        if (!estadoActual.IsName(ESTADO_IDLE))
        {
            animator.CrossFade(ESTADO_IDLE, velocidadTransicion);
        }

        corutinaGesto = null;
    }

    /// <summary>
    /// Fuerza la vuelta a Idle inmediatamente (con transición suave).
    /// </summary>
    private void VolverAIdleInmediato()
    {
        if (corutinaGesto != null)
        {
            StopCoroutine(corutinaGesto);
            corutinaGesto = null;
        }

        if (animator != null)
        {
            animator.speed = 1f;
            animator.CrossFade(ESTADO_IDLE, velocidadTransicion);
        }
    }
}
