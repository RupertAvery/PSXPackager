namespace Popstation.Notification
{
    public interface INotifier
    {
        void Notify(PopstationEventEnum @event, object value);
    }
}