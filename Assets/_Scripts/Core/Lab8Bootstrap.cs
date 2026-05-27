using UnityEngine;

namespace Lab8
{
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (FindObjectOfType<GameDirector>() != null)
            {
                return;
            }

            GameObject root = new GameObject("Lab8_GameDirector");
            root.AddComponent<GameDirector>();
        }
    }
}
