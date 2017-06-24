using System;
using NServiceBus;

class DurationProbe : Probe, IDurationProbe
{
    public DurationProbe(string name, string description) : base(name, description)
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