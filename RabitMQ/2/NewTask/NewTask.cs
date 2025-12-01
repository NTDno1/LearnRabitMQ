using RabbitMQ.Client;
using System.Text;

namespace RabbitMQDemo
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

            await channel.ExchangeDeclareAsync(
                exchange: "Test",
                type: ExchangeType.Direct,
                durable: false
            );

            await channel.QueueDeclareAsync(
                queue: "hello",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueBindAsync(
                queue: "hello",
                exchange: "Test",
                routingKey: "hello"
            );

            Console.WriteLine("=== Task Sender ===");
            Console.WriteLine("Type your task and press Enter. Type 'exit' to quit.\n");

            while (true)
            {
                Console.Write("> ");
                var message = Console.ReadLine();

                if (message == null || message.Trim().ToLower() == "exit")
                    break;

                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(
                    exchange: "Test",
                    routingKey: "hello",
                    body: body);

                Console.WriteLine($" [x] Sent: {message}");
            }

            Console.WriteLine("Sender stopped.");
        }
    }
}
