using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Worker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = "47.130.33.106",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "hello",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Tạo exchange kiểu direct
            await channel.ExchangeDeclareAsync(
                exchange: "Test",
                type: "direct",
                durable: false,
                autoDelete: false,
                arguments: null);
                
            // Bind queue với exchange + routing key
            await channel.QueueBindAsync(
                queue: "hello",
                exchange: "Test",
                routingKey: "hello"
            );

            // Async version của BasicQos
            await channel.BasicQosAsync(
                prefetchSize: 0, 
                prefetchCount: 1, 
                global: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] Received {message}");
                Console.WriteLine($"     RoutingKey: {ea.RoutingKey}");
                Console.WriteLine($"     Exchange: {ea.Exchange}");
                int dots = message.Split('.').Length - 1;
                await Task.Delay(dots * 1000);

                Console.WriteLine(" [x] Done");
                
                // Async version của BasicAck
                await channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag, 
                    multiple: false);
            };

            await channel.BasicConsumeAsync(
                queue: "hello",
                autoAck: false,
                consumer: consumer);

            Console.WriteLine(" [*] Waiting for messages. Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}