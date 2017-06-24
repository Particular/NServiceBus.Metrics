namespace NServiceBus
{
    using System;

    class DurationProbe : Probe, IDurationProbe
    {
        public DurationProbe(string name, string description) : base(name, description)
        {
        }

        internal void Record(TimeSpan duration)
        {
            observers(duration);
        }

        public void Register(Action<TimeSpan> observer)
        {
            observers += observer;
        }

        Action<TimeSpan> observers = span => { };
    }
}