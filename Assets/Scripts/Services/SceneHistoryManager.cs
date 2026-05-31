using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace LocalCalendar.Services
{
    public enum AppScene
    {
        None,
        Calendar,
        Agenda,
        EditItem,
        Schedule,
        Settings,
        Year
    }

    public interface IBackHandler
    {
        bool OnBackButtonPressed(); // Return true if it handled the click (consumed it)
    }

    public class SceneHistoryManager : MonoBehaviour
    {
        public static SceneHistoryManager Instance {
            get
            {
                // If we are quitting, don't try to create a new one!
                if (_isQuitting) return null;

                if (_instance == null)
                {
                    // If the instance is null, try to find it in the scene
                    _instance = Object.FindFirstObjectByType<SceneHistoryManager>();

                    // If it's not in the scene, load it from Resources.
                    if (_instance == null)
                    {
                        var prefab = Resources.Load<GameObject>("SceneHistoryManager");
                        if(prefab != null)
                        {
                            GameObject go = Instantiate(prefab);
                            _instance = go.GetComponent<SceneHistoryManager>();
                        }
                    }
                }
                return _instance;
            }
        }

        private static SceneHistoryManager _instance;

        public AppScene PreviousScene { get; private set; } = AppScene.None;
        public AppScene CurrentScene { get; private set; } = AppScene.None;

        // The current active back-button listener (popup or a scene controller)
        private IBackHandler _activeHandler;
        public void RegisterHandler(IBackHandler handler) => _activeHandler = handler;
        public void UnregisterHandler(IBackHandler handler) { if(_activeHandler == handler) _activeHandler = null; }

        private static bool _isQuitting = false;
        private void OnApplicationQuit() { _isQuitting = true; }

        // Use this to check if the manager is alive without creating a new one
        public static bool Exists => _instance != null && !_isQuitting;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                // Set initial scene
                CurrentScene = GetEnumFromSceneName(SceneManager.GetActiveScene().name);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (_instance != this)
            {
                // We already have a manager! Destroy this extra one.
                // Use DestroyImmediate to ensure it's gone before it can do anything else
                DestroyImmediate(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Clear the handler when moving to a new scene so we don't try to call a destroyed object (from the prev scene)
            _activeHandler = null;

            AppScene loadedScene = GetEnumFromSceneName(scene.name);

            // Don't update if we "loaded" the same scene (e.g. a refresh)
            if (loadedScene == CurrentScene) return;

            PreviousScene = CurrentScene;
            CurrentScene = loadedScene;
        }

        void Update()
        {
            void HandleBackButton()
            {
                // 1. Try to let a registered popup/view handle it
                if (_activeHandler != null && _activeHandler.OnBackButtonPressed())
                {
                    return; // The popup handled it, do nothing else
                }

                // 2. Fallback: Go to previous scene
                if (PreviousScene != AppScene.None)
                {
                    LoadScene(PreviousScene);
                }
            }

            // 'Escape' is the Back button on Android
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleBackButton();
            }
        }

        // --- Public interface ---

        // Helper to change scenes safely
        public void LoadScene(AppScene target)
        {
            string sceneName = GetSceneNameFromEnum(target);
            SceneManager.LoadScene(sceneName);
        }

        public void GoBack()
        {
            LoadScene(SceneHistoryManager.Instance.PreviousScene);
        }

        // --- Translation logic ---

        private string GetSceneNameFromEnum(AppScene scene)
        {
            return scene switch
            {
                AppScene.Calendar => "CalendarScene",
                AppScene.Agenda => "AgendaScene",
                AppScene.EditItem => "EditItemScene",
                AppScene.Schedule => "ScheduleScene",
                AppScene.Settings => "SettingsScene",
                AppScene.Year => "YearScene",
                _ => "CalendarScene" // Default fallback
            };
        }

        private AppScene GetEnumFromSceneName(string sceneName)
        {
            return sceneName switch
            {
                "CalendarScene" => AppScene.Calendar,
                "AgendaScene" => AppScene.Agenda,
                "EditItemScene" => AppScene.EditItem,
                "ScheduleScene" => AppScene.Schedule,
                "SettingsScene" => AppScene.Settings,
                "YearScene" => AppScene.Year,
                _ => AppScene.Calendar // Default fallback
            };
        }
    }
}
