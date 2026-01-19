# LocalCalendar (Unity)
A local-DB calendar and scheduling system built in Unity. Designed for reminders, repeating events, multi-day spans, and flexible UI presentation across month, day, and schedule views.

This project includes internal functionality for recurrence expansion, time-zone correctness, dynamic layout, and SQLite persistence.


## Concept
LocalCalendar is a custom calendar system implemented entirely in Unity. Instead of using iOS/Android calendar APIs, it stores events locally and renders them with Unity UI.

Core goals:
* Work fully offline.
* Support repeating and multi-day events.
* Provide multiple views (Month grid, Day popup, Schedule list).
* Handle real-world calendar complexity inside Unity's layout system.

The project intentionally treats calendar logic as data + expansion + presentation, rather than hard-coding behavior into UI.


## Features Implemented

### Calendar Views

#### Month Grid View
* 6×7 grid layout (42 cells).
* Dynamically sized DayCells.
* Highlights today.
* Shows items per day.
* Handles events spanning multiple days.

#### Day Popup View
* Click any day in the month grid to open this view.
* Shows expanded occurrences for that specific day.
* Supports repeating and multi-day events.

#### Schedule View
* Chronological list by day.
* Groups items per date.
* Shows occurrence times correctly per expansion.
* Displays multi-day continuation rows.


### Recurrence System

* Supports repeat rules (daily, weekly, monthly, yearly).
* Custom interval support.
* Optional Until date.
* Preserves original time-of-day for each occurrence.
* Expands occurrences inside a requested window instead of pre-materializing everything.

The system separates:
* Stored items (CalendarItem)
* Repeat rules (RepeatRule)
* Expansion logic (RecurrenceExpander)
* UI usage (GetExpandedMonthItems, GetExpandedDayItems)


### Time & Multi-Day Handling

* Stores times in UTC.
* Converts safely to local time.
* Preserves duration for multi-day events.
* Expands one occurrence into multiple visible day rows.

Correctly recognizes and handles multi-day events by:

* First day
* Middle days
* Last day


### Persistence
Uses SQLite via Unity.

Tables include:
* CalendarItemRow
* RepeatRuleRow
* ReminderRow

Maps relational rows into runtime CalendarItem objects.


### UI System

* Uses layout groups (GridLayoutGroup, HorizontalLayoutGroup, VerticalLayoutGroup)
* Dynamically resizes DayCells and DayEventRows.
* Supports compact and expanded row modes for DayEventRows.


### Reminders

* Supports reminder items.
* Distinguishes between event and reminder types.
* Integrates with Android system notifications.
* Designed for easy expansion with other system-notification systems.


### Architecture Overview
High-level flow:
```
Database → CalendarRepository → CalendarItem
          ↓
          RecurrenceExpander
          ↓
          GetExpandedMonthItems / GetExpandedDayItems
          ↓
          UI
```
Key pieces:
* CalendarRepository: Data access and filtering.
* RecurrenceExpander: Generates occurrences.
* CalendarUtils: Expansion helpers + duration logic.
* DayCell / DayEventRow: Presentation logic.

Expansion always happens at query time, not at storage time.


### Possible Future Improvements / Known Issues
* The text-input cursor still doesn't show up on Android.
* Notifications are a best-try case. Android doesn't guarantee timely notifications even with exact-alarm permissions and unrestricted battery usage.
* Time zone switching.
* Search-event functionality.
* UX niceties like multi-day bars in month grid, event colors, day and/or event emojis or stickers.

