using UnityEngine;

namespace Lab8
{
    public static class SaveSystem
    {
        private const string SaveKey = "LAB8_FULL_GAME_SAVE";

        public static void Save(SaveGameData data)
        {
            if (data == null)
            {
                return;
            }

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public static SaveGameData Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return null;
            }

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveGameData>(json);
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
