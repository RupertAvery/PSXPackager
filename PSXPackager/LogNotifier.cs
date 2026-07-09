using System;
using System.IO;
using Popstation;
using PSXPackager.Common;
using PSXPackager.Common.Notification;

namespace PSXPackager
{
    public class LogNotifier : NotifierBase
    {
        private readonly string _path;

        public LogNotifier(string path)
        {
            _path = path;
        }

        public override void Notify(PopstationEventEnum @event, object value)
        {

            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                    StartDateTime = DateTime.Now;
                    WriteLine(@event, $"Processing started: {StartDateTime.Hour:00}:{StartDateTime.Minute:00}:{StartDateTime.Second:00}");
                    break;

                case PopstationEventEnum.ProcessingComplete:
                    var elapsedSpan = DateTime.Now - StartDateTime;
                    WriteLine(@event, $"Processing completed in {elapsedSpan.TotalHours:00}h {elapsedSpan.Minutes:00}m {elapsedSpan.Seconds:00}s");
                    break;

                case PopstationEventEnum.Error:
                    WriteLine(@event, $"ERROR: {value}");
                    break;
                case PopstationEventEnum.Info:
                    WriteLine(@event, $"INFO: {value}");
                    break;
                case PopstationEventEnum.Warning:
                    WriteLine(@event, $"WARNING: {value}");
                    break;

                case PopstationEventEnum.ConvertStart:
                    WriteLine(@event, $"Converting Disc {value}");
                    break;
                case PopstationEventEnum.DiscStart:
                    WriteLine(@event, $"Writing Disc {value}");
                    break;
                case PopstationEventEnum.ExtractStart:
                    WriteLine(@event, $"Extracting Disc {value}");
                    break;
                case PopstationEventEnum.DecompressStart:
                    WriteLine(@event, $"Decompressing file {value}");
                    break;
            }
        }

        private void WriteLine(PopstationEventEnum @event, string text)
        {
            File.AppendAllText(_path, TimeStamp(@event) + text + "\r\n");
        }


        private string TimeStamp(PopstationEventEnum @event)
        {
            if (IsProgressEvent(@event)) return string.Empty;
            return FormatTimestamp(DateTime.Now);
        }
    }
}