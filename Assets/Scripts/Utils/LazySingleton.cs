using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class LazySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        // Returns the instance of this singleton
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (_instance == null)
                    {
                        var obj = new GameObject(string.Format("[LazySingleton] {0}", typeof(T).Name));
                        var comp = obj.AddComponent<T>();
                        _instance = comp;
                        return comp;
                    }
                }

                return _instance;
            }
        }
    }
}
