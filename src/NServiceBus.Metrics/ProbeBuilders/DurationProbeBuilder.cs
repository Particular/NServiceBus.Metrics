abstract class DurationProbeBuilder
{
    protected abstract void WireUp(DurationProbe probe);

    protected abstract string ProbeId { get; }

    public DurationProbe Build()
    {
        var probe = new DurationProbe(ProbeId);

        WireUp(probe);

        return probe;
    }
}