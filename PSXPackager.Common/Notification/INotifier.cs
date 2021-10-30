namespace PSXPackager.Common.Notification
{
    public interface INotifier
    {
        void Notify(PopstationEventEnum @event, object value);
    }
}