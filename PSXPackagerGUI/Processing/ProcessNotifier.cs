using System.Windows.Threading;
using PSXPackager.Common;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Models;
using PSXPackagerGUI.Pages;

namespace PSXPackagerGUI.Processing
{
    public class ProcessNotifier : INotifier
    {
        private readonly Dispatcher _dispatcher;
        private double _lastvalue;
        private string _action;
        private bool _cancelled;
        public ProcessNotifier(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public BatchEntryModel Entry { get; set; }

        public void Notify(PopstationEventEnum @event, object value)
        {
            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                    break;

                case PopstationEventEnum.ProcessingComplete:
                    Entry.MaxProgress = 100;
                    Entry.Progress = 0;
                    break;

                case PopstationEventEnum.Cancelled:
                    _cancelled = true;
                    break;

                case PopstationEventEnum.Error:
                    Entry.Status = "Error";
                    Entry.MaxProgress = 100;
                    Entry.Progress = 100;
                    Entry.HasError = true;
                    Entry.ErrorMessage += (string) value + "\r\n";

                    break;

                case PopstationEventEnum.FileName:
                case PopstationEventEnum.Info:
                    break;

                case PopstationEventEnum.Warning:
                    break;

                case PopstationEventEnum.GetIsoSize:
                    _lastvalue = 0;
                    Entry.MaxProgress = (uint)value;
                    Entry.Progress = 0;
                    break;

                case PopstationEventEnum.ConvertSize:
                case PopstationEventEnum.ExtractSize:
                case PopstationEventEnum.WriteSize:
                    _lastvalue = 0;
                    Entry.MaxProgress = (uint)value;
                    Entry.Progress = 0;
                    break;

                case PopstationEventEnum.ConvertStart:
                    _action = "Converting";
                    break;

                case PopstationEventEnum.DiscStart:
                    _action = $"Writing Disc {value}";

                    break;

                case PopstationEventEnum.ExtractStart:
                    _action = "Extracting";
                    break;

                case PopstationEventEnum.DecompressStart:
                    _action = "Decompressing";

                    break;

                case PopstationEventEnum.ExtractComplete:
                case PopstationEventEnum.DiscComplete:
                case PopstationEventEnum.DecompressComplete:
                    break;

                case PopstationEventEnum.ConvertComplete:
                    if (_cancelled)
                    {
                        Entry.Status = "Cancelled";
                    }
                    else
                    {
                        Entry.Status = "Complete";
                    }
                    break;


                case PopstationEventEnum.ConvertProgress:
                case PopstationEventEnum.ExtractProgress:
                case PopstationEventEnum.WriteProgress:
                    _dispatcher.Invoke(() =>
                    {
                        var percent = (uint)value / (float)Entry.MaxProgress * 100f;
                        if (percent - _lastvalue >= 0.25)
                        {
                            Entry.Status = $"{_action} ({percent:F0}%)";
                            Entry.Progress = (uint)value;
                            _lastvalue = percent;
                        }
                    });

                    break;

                case PopstationEventEnum.DecompressProgress:
                    break;
            }
        }
    }
}