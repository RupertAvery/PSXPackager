using System;
using System.IO;
using Popstation;

namespace PSXPackager
{
    public class LogNotifier : INotifier
    {
        private readonly string _path;
        private DateTime _startDateTime;

        public LogNotifier(string path)
        {
            _path = path;
        }

        public void Notify(PopstationEventEnum @event, object value)
        {
            
            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                    _startDateTime = DateTime.Now;
                    WriteLine(@event, $"Processing started: {_startDateTime.Hour:00}:{_startDateTime.Minute:00}:{_startDateTime.Second:00}");
                    break;

                case PopstationEventEnum.ProcessingComplete:
                    var elapsedSpan = DateTime.Now - _startDateTime;
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
                case PopstationEventEnum.WriteStart:
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
            var currentTime = DateTime.Now;
            switch (@event)
            {
                case PopstationEventEnum.ConvertProgress:
                case PopstationEventEnum.ExtractProgress:
                case PopstationEventEnum.WriteProgress:
                case PopstationEventEnum.DecompressProgress:
                    break;
                default:
                    return $"[{currentTime.Hour:00}:{currentTime.Minute:00}:{currentTime.Second:00}]: ";
            }
            return string.Empty;
        }
    }
}