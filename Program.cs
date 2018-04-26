using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Transports.InMemory;

namespace MassiveTransitDemo
{
    public class Program
    {
        public static async Task Main()
        {
            IHost inRamHost = null;

            var bus = Bus.Factory.CreateUsingInMemory(sbc =>
            {
                inRamHost = sbc.Host;

                sbc.ReceiveEndpoint("test_queue1", ep =>
                {
                    ep.Handler<YourMessage>(async (context) =>
                   {
                       await Console.Out.WriteLineAsync($"Preset Handler Received: \"{context.Message.Text}!\"");
                   });
                });
            });

            bus.Start();

            HostReceiveEndpointHandle receiveEndpoint = null;

            if (inRamHost != null && inRamHost is IInMemoryHost host)
            {
                receiveEndpoint = host.ConnectReceiveEndpoint("test_queue2", configurator =>
                {
                    configurator.Handler<YourMessage>(async context =>
                   {
                       await Console.Out.WriteLineAsync($"Dynamic Endpoint Received: \"{context.Message.Text}!\"");
                   });
                });
            }

            var endpoint1 = await bus.GetSendEndpoint(new Uri("loopback://localhost/test_queue1"));
            var endpoint2 = await bus.GetSendEndpoint(new Uri("loopback://localhost/test_queue2"));

            await Task.WhenAll(
                endpoint1.Send<YourMessage>(new YourMessage { Text = "Hello MassiveTransit" }),
                endpoint1.Send<YourMessage>(new YourMessage { Text = "Send again" }),
                endpoint2.Send<YourMessage>(new YourMessage { Text = "Hello MassiveTransit" }),
                endpoint2.Send<YourMessage>(new YourMessage { Text = "Send again" }));

            await Task.WhenAll(
                bus.Publish(new YourMessage { Text = "[1st] BroadCasting!" }),
                bus.Publish(new YourMessage { Text = "[2nd] BroadCasting again!!" }));

            Thread.Sleep(new TimeSpan(0,0,1));

            await receiveEndpoint?.StopAsync();

            await bus.Publish(new YourMessage { Text = "[3rd] Final BroadCasting!!!" });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await bus.StopAsync();
        }
    }

    public class YourMessage { public string Text { get; set; } }

    // public class MyConsumer : IConsumer<YourMessage>
    // {
    //     public async Task Consume(ConsumeContext<YourMessage> context)
    //     {
    //        await Console.Out.WriteLineAsync($"Text={context.Message.Text}");
    //     }
    // }

}