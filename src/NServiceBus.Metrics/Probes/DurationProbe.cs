using System;
using NServiceBus;

class DurationProbe : Probe, IDurationProbe
{
    public DurationProbe(string id, string name) : base(id, name)
    {
    }

    public void Register(Action<TimeSpan> observer)
    {
        observers += observer;
    }

    internal void Record(TimeSpan duration)
    {
        observers(duration);
    }

    Action<TimeSpan> observers = span => { };
}