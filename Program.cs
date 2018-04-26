using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Transports.InMemory;

namespace MassiveTransitDemo
{
    public class Program
    {
        public static void Main()
        {
            IHost inRamHost = null;

            var bus = Bus.Factory.CreateUsingInMemory(sbc =>
            {
                inRamHost = sbc.Host;

                sbc.ReceiveEndpoint("test_queue1", ep =>
                {
                    ep.Handler<YourMessage>(context =>
                    {
                        Console.Out.WriteLineAsync($"Preset Handler Received: \"{context.Message.Text}!\"");
                        return Task.CompletedTask;
                    });
                });
            });



            bus.Start();


            HostReceiveEndpointHandle receiveEndpoint = null;

            if (inRamHost != null && inRamHost is IInMemoryHost host)
            {
                receiveEndpoint = host.ConnectReceiveEndpoint("test_queue2", (configurator) =>
                {
                    configurator.Handler<YourMessage>(context =>
                    {
                        Console.Out.WriteLineAsync($"Dynamic Endpoint Received: \"{context.Message.Text}!\"");
                        return Task.CompletedTask;
                    });
                });
            }

            var endpoint1 = bus.GetSendEndpoint(new Uri("loopback://localhost/test_queue1")).Result;

            endpoint1.Send<YourMessage>(new YourMessage {Text = "Hello MassiveTransit"});
            endpoint1.Send<YourMessage>(new YourMessage { Text = "Send again" });

            var endpoint2 = bus.GetSendEndpoint(new Uri("loopback://localhost/test_queue2")).Result;

            endpoint2.Send<YourMessage>(new YourMessage { Text = "Hello MassiveTransit" });
            endpoint2.Send<YourMessage>(new YourMessage { Text = "Send again" });


            bus.Publish(new YourMessage { Text = "BroadCasting!!!" });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            receiveEndpoint?.StopAsync();

            bus.StopAsync();

        }
    }


    public class YourMessage { public string Text { get; set; } }


    public class MyConsumer : IConsumer<YourMessage>
    {
#pragma warning disable 1998
        public async Task Consume(ConsumeContext<YourMessage> context)
#pragma warning restore 1998
        {
            Console.WriteLine($"Text={context.Message.Text}");
        }
    }

}