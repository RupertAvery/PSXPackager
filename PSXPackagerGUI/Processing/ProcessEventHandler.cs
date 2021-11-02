using System;
using PSXPackager.Common;
using PSXPackager.Common.Notification;

namespace PSXPackagerGUI.Processing
{
    public class ProcessEventHandler : IEventHandler
    {
        public Action OverwriteAllSelected { get; set; }
        
        public Action CancelSelected { get; set; }
        
        public bool Cancelled { get; set; }
        
        public bool OverwriteIfExists { get; set; }

        public ActionIfFileExistsEnum ActionIfFileExists(string arg)
        {
            throw new NotImplementedException();
        }
    }
}