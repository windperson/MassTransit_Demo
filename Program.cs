using System;
using MassTransit;

namespace MassiveTransitDemo
{
    public class YourMessage { public string Text { get; set; } }
public class Program
{
    public static void Main()
    {
        var bus = Bus.Factory.CreateUsingInMemory(sbc =>
        {
            sbc.ReceiveEndpoint( "test_queue", ep =>
            {
                ep.Handler<YourMessage>(context =>
                {
                    return Console.Out.WriteLineAsync($"Received: \"{context.Message.Text}!\"");
                });
            });
        });

        bus.Start();

        bus.Publish(new YourMessage{Text = "Hello MassiveTransit"});
        bus.Publish(new YourMessage{Text = "In Memory Queue"});

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();

        bus.Stop();

        }
    }
}
