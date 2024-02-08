using System;

[AttributeUsage(AttributeTargets.Class)]
sealed class ProbePropertiesAttribute(string name, string description) : Attribute
{
    public readonly string Name = name;
    public readonly string Description = description;
}