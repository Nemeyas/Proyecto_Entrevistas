using UnityEngine;

/// <summary>
/// Rota de manera continua y estética un elemento UI para servir como indicador de carga.
/// </summary>
public class UISpinner : MonoBehaviour
{
    [Tooltip("Velocidad de rotación en grados por segundo.")]
    public float velocidadRotacion = 250f;

    void Update()
    {
        // Rota en el eje Z de forma constante e independiente del framerate
        transform.Rotate(0f, 0f, -velocidadRotacion * Time.deltaTime);
    }
}
