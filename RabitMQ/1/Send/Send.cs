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

const string message = "13";
var body = Encoding.UTF8.GetBytes(message);

await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "hello2", body: body);
Console.WriteLine($" [x] Sent {message}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();