using UnityEngine;
using UnityEngine.SceneManagement;

namespace LocalCalendar.Services
{
    public class SidePanelPopover : MonoBehaviour
    {
        public void OpenCalendar()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Calendar);
        }

        public void OpenSchedule()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Schedule);
        }

        public void OpenSettings()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Settings);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
