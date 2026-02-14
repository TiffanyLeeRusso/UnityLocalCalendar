using LocalCalendar.Services;
using System;

namespace LocalCalendar.Prefabs
{
    // To prevent overwrite/timing issues, these items should all be static.
    // Anything dynamic can be set directly on the instanced header obj.
    public class HeaderConfig
    {
        public string SceneTitle = "";
        public SidePanelPopover SideMenuPopover;
        public bool ShowSidePanel = true;
        public bool ShowBack = false;
        public bool ShowAdd = true;
        public bool ShowPrev = true;
        public bool ShowNext = true;
        public bool ShowToday = false;
    }
}
