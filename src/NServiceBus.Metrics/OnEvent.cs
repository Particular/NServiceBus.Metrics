namespace NServiceBus
{
    /// <summary>
    /// Represents an action taken when an event <paramref name="e"/> occurs.
    /// </summary>
    /// <typeparam name="TEventType">Event type</typeparam>
    /// <param name="e">The event.</param>
    public delegate void OnEvent<TEventType>(ref TEventType e);
}