using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Transports.InMemory;
using MassTransit.Transports.InMemory.Configuration;

namespace MassiveTransitDemo
{
    public class Program
    {
        public static async Task Main()
        {
            IHost ramHost1 = null, ramHost2 = null;

            var busControl1 = Bus.Factory.CreateUsingInMemory(sbc =>
            {
                ramHost1 = sbc.Host;

                sbc.ReceiveEndpoint("test_queue3", ConfigureHost1Endpoint);

            });

            var busControl2 = Bus.Factory.CreateUsingInMemory(configurator => { ramHost2 = configurator.Host; });

            busControl1.Start();
            busControl2.Start();

            HostReceiveEndpointHandle host1ReceiveEndpoint1 = null,
                host1ReceiveEndpoint2 = null,
                host2ReceiveEndpoint1 = null,
                host2ReceiveEndpoint2 = null;

            if (ramHost1 != null)
                if (ramHost1 is IInMemoryHost host)
                {
                    host1ReceiveEndpoint1 = host.ConnectReceiveEndpoint("test_queue1", ConfigHost1DynamicEndpoint1);

                    host1ReceiveEndpoint2 = host.ConnectReceiveEndpoint("test_queue2",
                        configurator =>
                        {
                            configurator.Handler<YourMessage>(async context =>
                            {
                                await Console.Out.WriteLineAsync(
                                    $"Dynamic Endpoint2 @ Host1 Received: \"{context.Message.Text}!\"");
                            });
                        });

                }

            if (ramHost2 != null)
                if (ramHost2 is IInMemoryHost host)
                {
                    host2ReceiveEndpoint1 = host.ConnectReceiveEndpoint("test_queue1", ConfigHost2DynamicEndpoint1);

                    host2ReceiveEndpoint2 = host.ConnectReceiveEndpoint("test_queue2", configurator =>
                    {
                        configurator.Handler<YourMessage>(async context =>
                        {
                            await Console.Out.WriteLineAsync(
                                $"Dynamic Endpoint2 @ Host2 Received: \"{context.Message.Text}!\"");
                        });
                    });
                }


            var endpoint1_1 = await busControl1.GetSendEndpoint(new Uri("loopback://localhost1/test_queue1"));
            var endpoint1_2 = await busControl1.GetSendEndpoint(new Uri("loopback://localhost1/test_queue2"));

            var endpoint2_1 = await busControl2.GetSendEndpoint(new Uri("loopback://localhost2/test_queue1"));
            var endpoint2_2 = await busControl2.GetSendEndpoint(new Uri("loopback://localhost2/test_queue2"));

            await Task.WhenAll(
                Task.Run(async () =>
                {
                    await endpoint1_1.Send<YourMessage>(new YourMessage { Text = "Hello MassiveTransit" });
                    await endpoint1_1.Send<YourMessage>(new YourMessage { Text = "Send again" });
                }),

                Task.Run(async () =>
                {
                    await endpoint1_2.Send<YourMessage>(new YourMessage { Text = "Hello MassiveTransit" });
                    await endpoint1_2.Send<YourMessage>(new YourMessage { Text = "Send again" });
                }),

                Task.Run(async () =>
                {
                    await endpoint2_1.Send<YourMessage>(new YourMessage { Text = "Host2 endPoint1 Hello Message" });
                    await endpoint2_1.Send<YourMessage>(new YourMessage { Text = "Host2 endPoint1 send again" });
                }),

                Task.Run(async () =>
                {
                    await endpoint2_2.Send<YourMessage>(new YourMessage { Text = "Host2 endPoint2 Hello Message" });
                    await endpoint2_2.Send<YourMessage>(new YourMessage { Text = "Host2 endPoint2 send again" });
                })
            );

            await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        await busControl1.Publish(new YourMessage { Text = "[1st] BUS_1 BroadCasting!" });
                        await busControl1.Publish(new YourMessage { Text = "[2nd] BUS_1 BroadCasting again!!" });
                    }),
                    Task.Run(async () =>
                    {
                        await busControl2.Publish(new YourMessage { Text = "[1st] BUS_2 BroadCasting!" });
                        await busControl2.Publish(new YourMessage { Text = "[2nd] BUS_2 BroadCasting again!!" });
                    })
            );

            Thread.Sleep(new TimeSpan(0, 0, 1));

            await host1ReceiveEndpoint1?.StopAsync();
            await host1ReceiveEndpoint2?.StopAsync();
            await host2ReceiveEndpoint1?.StopAsync();

            await Task.WhenAll(
                busControl1.Publish(new YourMessage {Text = "[3rd] BUS_1 Final BroadCasting!!!"}),
                busControl2.Publish(new YourMessage {Text = "[3rd] BUS_2 Final BroadCasting!!!"})
            );

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await busControl1.StopAsync();
            await busControl2.StopAsync();
        }

        private static void ConfigHost1DynamicEndpoint1(IReceiveEndpointConfigurator configurator)
        {
            configurator.Consumer<MyConsumer>(() => { return new MyConsumer("host1", "001"); });
        }

        private static void ConfigHost2DynamicEndpoint1(IReceiveEndpointConfigurator configurator)
        {
            configurator.Consumer<MyConsumer>(() => { return new MyConsumer("host2", "001"); });
        }

        private static void ConfigureHost1Endpoint(IReceiveEndpointConfigurator configurator)
        {
            configurator.Consumer<MyConsumer>(() =>
            {
                return new MyConsumer("host1", "000");
            });
        }
    }

    public class YourMessage { public string Text { get; set; } }


    public class MyConsumer : IConsumer<YourMessage>
    {
        private string _hostId;
        private string _consumerId;
        public MyConsumer(string hostId, string consumerId)
        {
            _hostId = hostId;
            _consumerId = consumerId;
        }

        public async Task Consume(ConsumeContext<YourMessage> context)
        {
            await Console.Out.WriteLineAsync($"Host={_hostId}  Consumer={_consumerId} Text={context.Message.Text}");
        }
    }

}