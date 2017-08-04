using System;

[AttributeUsage(AttributeTargets.Class)]
sealed class ProbePropertiesAttribute : Attribute
{  
    public ProbePropertiesAttribute(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
}