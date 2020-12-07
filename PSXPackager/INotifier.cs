using Popstation;

namespace PSXPackager
{
    public interface INotifier
    {
        void Notify(PopstationEventEnum @event, object value);
    }
}