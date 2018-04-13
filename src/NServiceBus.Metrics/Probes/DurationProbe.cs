using System;
using NServiceBus;

class DurationProbe : Probe, IDurationProbe, IEventSensor<DurationEvent>
{
    public DurationProbe(string name, string description) : base(name, description)
    {
    }

    public void Register(OnEvent<DurationEvent> observer)
    {
        lock (Lock)
        {
            observers += observer;
        }
    }

    public void Register(Action<TimeSpan> observer)
    {
        Register((ref DurationEvent e) => observer(e.Duration));
    }

    public void Record(ref DurationEvent e)
    {
        observers(ref e);
    }

    volatile OnEvent<DurationEvent> observers = Empty;
    readonly object Lock = new object();

    static void Empty(ref DurationEvent e) { }
}