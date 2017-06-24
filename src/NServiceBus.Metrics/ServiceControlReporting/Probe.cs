namespace NServiceBus
{
    abstract class Probe
    {
        public string Name { get; }

        public string Description { get; }

        protected Probe(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}