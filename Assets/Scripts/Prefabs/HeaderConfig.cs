using LocalCalendar.Services;
using System;

namespace LocalCalendar.Prefabs
{
    public class HeaderConfig
    {
        public string Title;
        public string SceneTitle;
        public DateTime CurrentDate;
        public SidePanelPopover SideMenuPopover;
        public bool ShowSidePanel = true;
        public bool ShowBack = false;
        public bool ShowAdd = true;
        public bool ShowPrev = true;
        public bool ShowNext = true;
        public bool ShowToday = false;
    }
}
