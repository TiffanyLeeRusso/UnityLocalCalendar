using UnityEngine;
using System;
using System.Collections;

namespace LocalCalendar.Services
{
    public class LayoutWatcher : MonoBehaviour
    {
        public static LayoutWatcher Instance
        {
            get
            {
                if (_isQuitting) return null;

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LayoutWatcher>();

                    if (_instance == null)
                    {
                        var prefab = Resources.Load<GameObject>("LayoutWatcher");
                        if (prefab != null)
                        {
                            GameObject go = Instantiate(prefab);
                            _instance = go.GetComponent<LayoutWatcher>();
                        }
                    }
                }

                return _instance;
            }
        }

        private static LayoutWatcher _instance;
        private static bool _isQuitting;

        public event Action OnRelayout;

        RectTransform _watchRoot;
        Vector2 _lastSize;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            ResolveRoot();
        }

        void ResolveRoot()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                _watchRoot = canvas.GetComponent<RectTransform>();
                _lastSize = _watchRoot.rect.size;
            }
        }

        void Update()
        {
            if (_watchRoot == null)
            {
                ResolveRoot();
                return;
            }

            Vector2 size = _watchRoot.rect.size;

            if (Vector2.Distance(size, _lastSize) > 0.5f)
            {
                _lastSize = size;
                OnRelayout?.Invoke();
            }
        }

        void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
