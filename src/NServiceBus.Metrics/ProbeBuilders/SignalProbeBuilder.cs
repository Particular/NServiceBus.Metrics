abstract class SignalProbeBuilder
{
    protected abstract void WireUp(SignalProbe probe);

    protected abstract string ProbeId { get; }

    public SignalProbe Build()
    {
        var probe = new SignalProbe(ProbeId);

        WireUp(probe);

        return probe;
    }
}