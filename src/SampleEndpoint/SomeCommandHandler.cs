using System;
using System.Threading.Tasks;
using NServiceBus;

class SomeCommandHandler : IHandleMessages<SomeCommand>
{
    public Task Handle(SomeCommand message, IMessageHandlerContext context)
    {
        //throw new Exception("boom!");

        return Task.Delay(TimeSpan.FromSeconds(2));
    } 
}