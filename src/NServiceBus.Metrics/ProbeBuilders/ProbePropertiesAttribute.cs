using System;

[AttributeUsage(AttributeTargets.Class)]
sealed class ProbePropertiesAttribute : Attribute
{
    public ProbePropertiesAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public readonly string Name;
    public readonly string Description;
}