using System;

[AttributeUsage(AttributeTargets.Class)]
sealed class ProbePropertiesAttribute : Attribute
{
    public ProbePropertiesAttribute(ProbeType type, string name, string description)
    {
        Type = type;
        Name = name;
        Description = description;
    }

    public readonly ProbeType Type;
    public readonly string Name;
    public readonly string Description;
}

enum ProbeType
{
    Signal,
    Duration
}