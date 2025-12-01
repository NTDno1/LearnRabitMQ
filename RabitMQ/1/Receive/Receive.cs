using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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

// 1. Khai báo Exchange
await channel.ExchangeDeclareAsync(
    exchange: "Test1Exchange",
    type: ExchangeType.Direct,
    durable: true
);

// 2. Khai báo Queue (consumer chỉ nghe queue này)
var queueName = "hello1QueueName";

await channel.QueueDeclareAsync(
    queue: queueName,
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// 3. Bind queue vào exchange + routing key bạn muốn
await channel.QueueBindAsync(
    queue: queueName,
    exchange: "Test1Exchange",
    routingKey: "hello1RoutingKey"
);

Console.WriteLine(" [*] Waiting for messages...");

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    Console.WriteLine($" [x] Received message: {message}");
    Console.WriteLine($"     RoutingKey: {ea.RoutingKey}");
    Console.WriteLine($"     Exchange: {ea.Exchange}");
    return Task.CompletedTask;
};

// 4. Consume queue (queue đã được bind theo routing key)
await channel.BasicConsumeAsync(
    queue: queueName,
    autoAck: true,
    consumer: consumer
);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
