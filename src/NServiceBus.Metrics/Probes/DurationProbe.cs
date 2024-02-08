using System;
using NServiceBus;

class DurationProbe(string name, string description) : Probe(name, description), IDurationProbe
{
    public void Register(OnEvent<DurationEvent> observer)
    {
        lock (Lock)
        {
            observers += observer;
        }
    }

    public void Register(Action<TimeSpan> observer) => Register((ref DurationEvent e) => observer(e.Duration));

    internal void Record(ref DurationEvent e) => observers(ref e);

    volatile OnEvent<DurationEvent> observers = Empty;
    readonly object Lock = new();

    static void Empty(ref DurationEvent e) { }
}