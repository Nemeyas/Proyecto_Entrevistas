using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildProject
{
    /// <summary>
    /// Compila el proyecto como ejecutable Windows 64-bit.
    /// Se invoca desde la línea de comandos con:
    /// Unity.exe -batchmode -quit -projectPath "..." -executeMethod BuildProject.BuildWindows
    /// </summary>
    [MenuItem("Build/Compilar Ejecutable Windows")]
    public static void BuildWindows()
    {
        string[] escenas = new string[]
        {
            "Assets/Scenes/EntrevistaIA.unity"
        };

        // La carpeta de salida queda al lado de Frontend_Unity, en la raíz del proyecto
        string rutaSalida = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Build", "EntrevistaIA.exe"));

        // Crear la carpeta Build si no existe
        string carpetaBuild = Path.GetDirectoryName(rutaSalida);
        if (!Directory.Exists(carpetaBuild))
        {
            Directory.CreateDirectory(carpetaBuild);
        }

        Debug.Log($"[BuildProject] Compilando a: {rutaSalida}");

        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = rutaSalida,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var resultado = BuildPipeline.BuildPlayer(opciones);

        if (resultado.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildProject] ¡Compilación exitosa! Tamaño: {resultado.summary.totalSize / (1024 * 1024)} MB");
            Debug.Log($"[BuildProject] Ejecutable en: {rutaSalida}");

            // Copiar MESA DEFINITIVA.png a la carpeta EntrevistaIA_Data del build
            string origenMesa = Path.Combine(Application.dataPath, "MESA DEFINITIVA.png");
            string destinoMesa = Path.Combine(carpetaBuild, "EntrevistaIA_Data", "MESA DEFINITIVA.png");
            if (File.Exists(origenMesa))
            {
                try
                {
                    File.Copy(origenMesa, destinoMesa, true);
                    Debug.Log($"[BuildProject] Copiada MESA DEFINITIVA.png a {destinoMesa}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BuildProject] Error al copiar MESA DEFINITIVA.png: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[BuildProject] No se encontro MESA DEFINITIVA.png en {origenMesa}");
            }
        }
        else
        {
            Debug.LogError($"[BuildProject] Error en la compilación: {resultado.summary.result}");
            // Si se ejecuta en batchmode, salir con código de error
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
