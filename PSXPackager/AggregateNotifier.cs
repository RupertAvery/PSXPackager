using System.Collections.Generic;
using Popstation;
using Popstation.Notification;

namespace PSXPackager
{
    public class AggregateNotifier : INotifier
    {
        private readonly List<INotifier> _notifiers;
        public AggregateNotifier()
        {
            _notifiers = new List<INotifier>();
        }

        public void Add(INotifier notifier)
        {
            _notifiers.Add(notifier);
        }

        public void Notify(PopstationEventEnum @event, object value)
        {
            foreach (var notifier in _notifiers)
            {
                notifier.Notify(@event, value);
            }
        }
    }
}