using System;
using PSXPackager.Common;
using PSXPackager.Common.Notification;

namespace PSXPackager
{
    public class EventHandler: IEventHandler
    {
        public Action OverwriteAllSelected { get; set; }
        public Action CancelSelected { get; set; }
        public bool Cancelled { get; set; }
        public bool OverwriteIfExists { get; set; }

        public ActionIfFileExistsEnum ActionIfFileExists(string arg)
        {
            while (true)
            {
                Console.CursorVisible = true;
                Console.Write($"\r\n{arg} alreasy exists. Overwrite? (Y)es|(N)o|(A)ll|(C)ancel ");
                var key = Console.ReadKey();

                Console.WriteLine();

                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        
                        return ActionIfFileExistsEnum.Overwrite;
                    case ConsoleKey.N:
                        return ActionIfFileExistsEnum.Skip;
                    case ConsoleKey.A:
                        OverwriteIfExists = true;
                        OverwriteAllSelected?.Invoke();
                        return ActionIfFileExistsEnum.OverwriteAll;
                    case ConsoleKey.C:
                        Cancelled = true;
                        CancelSelected?.Invoke();
                        return ActionIfFileExistsEnum.Abort;
                }
            }
        }
    }
}