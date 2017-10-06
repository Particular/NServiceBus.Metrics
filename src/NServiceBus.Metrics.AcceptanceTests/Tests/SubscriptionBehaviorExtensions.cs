namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Pipeline;
    using Transport;

    static class SubscriptionBehaviorExtensions
    {
        public class SubscriptionEventArgs
        {
            /// <summary>
            /// The address of the subscriber.
            /// </summary>
            public string SubscriberReturnAddress { get; set; }

            /// <summary>
            /// The type of message the client subscribed to.
            /// </summary>
            public string MessageType { get; set; }
        }

        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context);
            }, "Provides notifications when endpoints subscribe");
        }

        class SubscriptionBehavior<TContext> : IBehavior<ITransportReceiveContext, ITransportReceiveContext> where TContext : ScenarioContext
        {
            Action<SubscriptionEventArgs, TContext> action;
            TContext scenarioContext;

            public SubscriptionBehavior(Action<SubscriptionEventArgs, TContext> action, TContext scenarioContext)
            {
                this.action = action;
                this.scenarioContext = scenarioContext;
            }

            public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                await next(context).ConfigureAwait(false);
                var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.Message);
                if (subscriptionMessageType != null)
                {
                    if (!context.Message.Headers.TryGetValue(Headers.SubscriberTransportAddress, out var returnAddress))
                    {
                        context.Message.Headers.TryGetValue(Headers.ReplyToAddress, out returnAddress);
                    }
                    action(new SubscriptionEventArgs
                    {
                        MessageType = subscriptionMessageType,
                        SubscriberReturnAddress = returnAddress
                    }, scenarioContext);
                }
            }

            static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
            {
                return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
            }
        }
    }
}