using System;

[AttributeUsage(AttributeTargets.Class)]
sealed class ProbePropertiesAttribute : Attribute
{
    public ProbePropertiesAttribute(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public readonly string Id;
    public readonly string Name;
}