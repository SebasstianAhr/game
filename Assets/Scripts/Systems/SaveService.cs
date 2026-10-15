using System;
using System.IO;
using UnityEngine;

namespace OnePercent
{
    /// <summary>Lee y escribe el progreso en Application.persistentDataPath/onepercent_save.json.</summary>
    public static class SaveService
    {
        private const string FileName = "onepercent_save.json";

        private static string FilePath
        {
            get { return System.IO.Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null)
                    {
                        data.EnsureValid();
                        return data;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[1% de Probabilidad] No se pudo leer el guardado, se empezará de cero. Detalle: " + exception.Message);
            }

            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[1% de Probabilidad] No se pudo guardar el progreso. Detalle: " + exception.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[1% de Probabilidad] No se pudo borrar el guardado. Detalle: " + exception.Message);
            }
        }
    }
}
