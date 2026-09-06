using System;
using PSXPackager.Common;
using PSXPackager.Common.Notification;

namespace PSXPackager
{
    public abstract class NotifierBase : INotifier
    {
        protected DateTime StartDateTime;

        public abstract void Notify(PopstationEventEnum @event, object value);

        protected static bool IsProgressEvent(PopstationEventEnum @event)
        {
            switch (@event)
            {
                case PopstationEventEnum.ConvertProgress:
                case PopstationEventEnum.ExtractProgress:
                case PopstationEventEnum.WriteProgress:
                case PopstationEventEnum.DecompressProgress:
                    return true;
                default:
                    return false;
            }
        }

        protected static string FormatTimestamp(DateTime time)
        {
            return $"[{time.Hour:00}:{time.Minute:00}:{time.Second:00}]: ";
        }
    }
}
