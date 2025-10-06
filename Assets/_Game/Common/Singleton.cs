using UnityEngine;
namespace master
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        Debug.LogError($"Instance doesn't exist {typeof(T).Name}");
                        return null;
                    }
                    return instance;
                }
                else
                {
                    return instance;
                }
            }
        }
        protected virtual void Awake()
        {
            instance = GetComponent<T>();
        }
    }
    public class SingletonAutoCreate<T> : MonoBehaviour where T : Component
    {
        static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        Setup();
                    }
                }

                return instance;
            }
            private set { instance = value; }
        }

        // Spawn instance
        static void Setup()
        {
            instance = new GameObject(typeof(T).Name).AddComponent<T>();
        }
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    Setup();
                }
            }
        }
    }

    public class SingletonDDOL<T> : MonoBehaviour
    {
        private static T instance;
        public static T Instance => instance;
        protected virtual void Awake()
        {
            if (instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = GetComponent<T>();
            if (transform.root == this.transform)
            {
                DontDestroyOnLoad(this);
            }
        }
    }
    public class SingletonDDOLAutoCreate<T> : MonoBehaviour where T : Component
    {
        static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null) Setup();

                return instance;
            }
            private set { instance = value; }
        }

        // Spawn instance
        static void Setup()
        {
            instance = new GameObject(typeof(T).Name + "DDOL").AddComponent<T>();
            DontDestroyOnLoad(instance);
        }
    }
    public class SingletonSO<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;
        public static T Instance => instance ?? (instance = Resources.Load<T>(typeof(T).Name));
    }

}
