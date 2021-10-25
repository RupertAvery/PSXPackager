using System;

namespace Popstation.Notification
{
    public interface IEventHandler
    {
        Action OverwriteAllSelected { get; set; }
        Action CancelSelected { get; set; }
        bool Cancelled { get; set; }
        bool OverwriteIfExists { get; set; }
        ActionIfFileExistsEnum ActionIfFileExists(string arg);
    }
}