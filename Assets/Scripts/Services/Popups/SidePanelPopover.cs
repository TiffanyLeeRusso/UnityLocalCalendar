using UnityEngine;
using UnityEngine.SceneManagement;

namespace LocalCalendar.Services
{
    public class SidePanelPopover : MonoBehaviour
    {
        public void OpenCalendar()
        {
            SceneManager.LoadScene("CalendarScene");
        }

        public void OpenSchedule()
        {
            // Pass the currently displayed calendar month
            //ScheduleContext.InitialMonth =
            //    new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            SceneManager.LoadScene("ScheduleScene");
        }

        public void OpenSettings()
        {
            SceneManager.LoadScene("SettingsScene");
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
