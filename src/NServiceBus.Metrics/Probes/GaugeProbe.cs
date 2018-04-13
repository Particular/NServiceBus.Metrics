using NServiceBus;

class GaugeProbe : Probe, IGaugeProbe, IEventSensor<GaugeEvent>
{
    public GaugeProbe(string name, string description) : base(name, description)
    {
    }

    public void Register(OnEvent<GaugeEvent> observer)
    {
        lock (Lock)
        {
            observers += observer;
        }
    }

    public void Record(ref GaugeEvent e)
    {
        observers(ref e);
    }

    volatile OnEvent<GaugeEvent> observers = Empty;
    readonly object Lock = new object();

    static void Empty(ref GaugeEvent e) { }
}