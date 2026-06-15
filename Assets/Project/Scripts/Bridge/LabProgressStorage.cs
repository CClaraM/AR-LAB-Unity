using System;
using System.IO;
using UnityEngine;

public static class LabProgressStorage
{
    private const string FolderName = "LabProgress";

    public static bool HasProgress(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            return false;

        return File.Exists(GetProgressPath(runId));
    }

    public static LabLocalProgress Load(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            return null;

        string path = GetProgressPath(runId);

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<LabLocalProgress>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Error cargando progreso local: " + e.Message);
            return null;
        }
    }

    public static void Save(LabLocalProgress progress)
    {
        if (progress == null || string.IsNullOrEmpty(progress.runId))
            return;

        try
        {
            string folder = GetProgressFolder();

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            progress.updatedAt = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(progress, true);
            File.WriteAllText(GetProgressPath(progress.runId), json);

            Debug.Log("Progreso local guardado: " + GetProgressPath(progress.runId));
        }
        catch (Exception e)
        {
            Debug.LogError("Error guardando progreso local: " + e.Message);
        }
    }

    public static void Delete(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            return;

        string path = GetProgressPath(runId);

        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
            Debug.Log("Progreso local eliminado: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Error eliminando progreso local: " + e.Message);
        }
    }

    private static string GetProgressFolder()
    {
        return Path.Combine(Application.persistentDataPath, FolderName);
    }

    private static string GetProgressPath(string runId)
    {
        string safeRunId = MakeSafeFileName(runId);
        return Path.Combine(GetProgressFolder(), safeRunId + ".json");
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return value;
    }
}