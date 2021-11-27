using System;
using Popstation;
using PSXPackager.Common;
using PSXPackager.Common.Notification;

namespace PSXPackager
{
    public class ConsoleNotifier : INotifier
    {
        private int _cursorYPos;
        private long _total;
        private long _lastTicks;
        private int _charsToDelete;
        private DateTime _startDateTime;
        private readonly int _logLevel;

        public ConsoleNotifier(int logLevel)
        {
            _logLevel = logLevel;
        }

        private int GetLogLevel(PopstationEventEnum @event)
        {
            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                case PopstationEventEnum.ProcessingComplete:
                    return -1;

                case PopstationEventEnum.Error:
                    return -1;

                case PopstationEventEnum.Warning:
                    return 1;

                case PopstationEventEnum.Info:
                    return 3;


                case PopstationEventEnum.FileName:
                    return 0;

                case PopstationEventEnum.GetIsoSize:
                case PopstationEventEnum.ConvertSize:
                case PopstationEventEnum.ExtractSize:
                case PopstationEventEnum.WriteSize:
                    return -1;

                case PopstationEventEnum.ConvertStart:
                case PopstationEventEnum.DiscStart:
                case PopstationEventEnum.ExtractStart:
                case PopstationEventEnum.DecompressStart:

                case PopstationEventEnum.ConvertProgress:
                case PopstationEventEnum.ExtractProgress:
                case PopstationEventEnum.WriteProgress:
                case PopstationEventEnum.DecompressProgress:

                case PopstationEventEnum.ConvertComplete:
                case PopstationEventEnum.ExtractComplete:
                case PopstationEventEnum.DiscComplete:
                case PopstationEventEnum.DecompressComplete:
                    return 2;
            }
            return -1;
        }

        public void Notify(PopstationEventEnum @event, object value)
        {
            if (GetLogLevel(@event) > _logLevel) return;

            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                    _startDateTime = DateTime.Now;
                    WriteLine(@event, $"Processing started: {_startDateTime.Hour:00}:{_startDateTime.Minute:00}:{_startDateTime.Second:00}");
                    break;

                case PopstationEventEnum.ProcessingComplete:
                    var elapsedSpan = DateTime.Now - _startDateTime;
                    WriteLine(@event, $"Processing completed: {elapsedSpan.TotalHours:00}h {elapsedSpan.Minutes:00}m {elapsedSpan.Seconds:00}s");
                    break;

                case PopstationEventEnum.Error:
                    _charsToDelete = 0;
                    Console.CursorVisible = true;
                    WriteLine(@event, $"\r\n{value}");
                    break;

                case PopstationEventEnum.FileName:
                case PopstationEventEnum.Info:
                    WriteLine(@event, $"{value}");
                    break;

                case PopstationEventEnum.Warning:
                    var lastColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    WriteLine(@event, $"WARNING: {value}");
                    Console.ForegroundColor = lastColor;
                    break;

                case PopstationEventEnum.GetIsoSize:
                    _total = Convert.ToInt64(value);
                    break;

                case PopstationEventEnum.ConvertSize:
                case PopstationEventEnum.ExtractSize:
                case PopstationEventEnum.WriteSize:
                    _total = Convert.ToInt64(value);
                    break;

                case PopstationEventEnum.ConvertStart:
                    Write(@event, $"Converting Disc {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;

                case PopstationEventEnum.DiscStart:
                    Write(@event, $"Writing Disc {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;

                case PopstationEventEnum.ExtractStart:
                    Write(@event, $"Extracting Disc {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;

                case PopstationEventEnum.DecompressStart:
                    Write(@event, $"Decompressing file {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;

                case PopstationEventEnum.ConvertComplete:
                case PopstationEventEnum.ExtractComplete:
                case PopstationEventEnum.DiscComplete:
                case PopstationEventEnum.DecompressComplete:
                    _charsToDelete = 0;
                    Console.CursorVisible = true;
                    Console.WriteLine();
                    break;

                case PopstationEventEnum.ConvertProgress:
                case PopstationEventEnum.ExtractProgress:
                case PopstationEventEnum.WriteProgress:
                    //Console.SetCursorPosition(0, _cursorYPos);
                    if (DateTime.Now.Ticks - _lastTicks > 100000)
                    {
                        Overwrite($"{Math.Round(Convert.ToInt32(value) / (double)_total * 100, 0) }%");
                        _lastTicks = DateTime.Now.Ticks;
                    }
                    break;
                case PopstationEventEnum.DecompressProgress:
                    //Console.SetCursorPosition(0, _cursorYPos);
                    if (DateTime.Now.Ticks - _lastTicks > 100000)
                    {
                        Overwrite($"{value}%");
                        _lastTicks = DateTime.Now.Ticks;
                    }
                    break;
            }
        }

        private string TimeStamp(PopstationEventEnum @event)
        {
            if (_logLevel < 4) return string.Empty;
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

        private void WriteLine(PopstationEventEnum @event, string text)
        {
            Console.WriteLine(TimeStamp(@event) + text);
        }

        private void Write(PopstationEventEnum @event, string text)
        {
            Console.Write(TimeStamp(@event) + text);
        }

        private void Overwrite(string text)
        {
            Console.Write(new string('\b', _charsToDelete));
            Console.Write(text);
            _charsToDelete = text.Length;
        }

    }
}