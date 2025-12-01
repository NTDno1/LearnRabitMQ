using RabbitMQ.Client;
using System.Text;

var factory = new ConnectionFactory
{
    HostName = "47.130.33.106",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// await channel.QueueDeclareAsync(queue: "hello1", durable: false, exclusive: false, autoDelete: false,
//     arguments: null);

Console.WriteLine("Type a message and press [Enter] to send. Press [Enter] on an empty line to exit.");

while (true)
{
    Console.Write("Input message: ");
    var message = Console.ReadLine();

    if (string.IsNullOrEmpty(message))
        break;

    var body = Encoding.UTF8.GetBytes(message);

    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "hello1", body: body);
    Console.WriteLine($" [x] Sent {message}");
}