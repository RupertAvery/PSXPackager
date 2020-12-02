using System;
using Popstation;

namespace PSXPackager
{
    public class ConsoleNotifications
    {
        private int _cursorYPos;
        private long _total;
        private long _lastTicks;
        private int _charsToDelete;

        public Action OverwriteAllSelected { get; set; }
        public Action CancelSelected { get; set; }

        public void Notify(PopstationEventEnum @event, object value)
        {
            switch (@event)
            {
                case PopstationEventEnum.Info:
                    Console.WriteLine($"{value}");
                    break;
                case PopstationEventEnum.Warning:
                    Console.WriteLine($"WARNING: {value}");
                    break;

                case PopstationEventEnum.GetIsoSize:
                    _total = Convert.ToInt64(value);
                    break;
                case PopstationEventEnum.ConvertSize:
                case PopstationEventEnum.WriteSize:
                    _total = Convert.ToInt64(value);
                    break;

                case PopstationEventEnum.ConvertStart:
                    Console.Write($"Converting Disc {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;
                case PopstationEventEnum.WriteStart:
                    Console.Write($"Writing Disc {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;
                case PopstationEventEnum.ExtractStart:
                    Console.Write($"Extracting Disc {value} - ");
                    _cursorYPos = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;

                case PopstationEventEnum.ConvertComplete:
                case PopstationEventEnum.ExtractComplete:
                case PopstationEventEnum.WriteComplete:
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
            }
        }

        public void Overwrite(string text)
        {
            Console.Write(new string('\b', _charsToDelete));
            Console.Write(text);
            _charsToDelete = text.Length;
        }

        public ActionIfFileExistsEnum ActionIfFileExists(string arg)
        {
            while (true)
            {
                Console.WriteLine($"{arg} alreasy exists. Overwrite? (Y)es|(N)o|(A)ll|(C)ancel");
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        return ActionIfFileExistsEnum.Overwrite;
                    case ConsoleKey.N:
                        return ActionIfFileExistsEnum.Skip;
                    case ConsoleKey.A:
                        OverwriteAllSelected?.Invoke();
                        return ActionIfFileExistsEnum.OverwriteAll;
                    case ConsoleKey.C:
                        CancelSelected?.Invoke();
                        return ActionIfFileExistsEnum.Abort;
                }
            }
        }
    }
}