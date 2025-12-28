using UnityEngine;
using LocalCalendar.Data;

namespace LocalCalendar.App
{
    public class AppBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Database.Initialize();
            DontDestroyOnLoad(gameObject);
        }
    }
}
