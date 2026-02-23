using System;

public enum LogCategory
{
    App,
    UI,
    DB,
    Notification,
    System
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}

[Serializable]
public class AppLogEntry
{
    public DateTime TimeUtc;
    public LogCategory Category;
    public LogLevel Level;
    public string Message;
}
