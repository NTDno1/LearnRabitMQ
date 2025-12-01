using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;
using System;

// --- 1. THIẾT LẬP KẾT NỐI VÀ KÊNH (CONNECTION & CHANNEL) ---
var factory = new ConnectionFactory
{
    HostName = "47.130.33.106",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// Tên Exchange và Queue được sử dụng
const string ExchangeName = "Test2Exchange";
const string QueueName = "hello1QueueName";
const string RoutingKey = "hello1RoutingKey";

// --- 2. KHAI BÁO CÁC THÀNH PHẦN (EXCHANGE, QUEUE, BINDING) ---

// Khai báo Exchange: Đảm bảo Exchange tồn tại
await channel.ExchangeDeclareAsync(
    exchange: ExchangeName,
    type: ExchangeType.Direct, // Chọn loại Direct Exchange
    durable: true
);

// Khai báo Queue: Đảm bảo Queue tồn tại
await channel.QueueDeclareAsync(
    queue: QueueName,
    durable: false, 
    exclusive: false, 
    autoDelete: false,
    arguments: null
);

// Tạo Binding: Liên kết Exchange với Queue bằng Routing Key
// Đây là bước quan trọng để định tuyến tin nhắn
await channel.QueueBindAsync(
    queue: QueueName,
    exchange: ExchangeName,
    routingKey: RoutingKey
);

Console.WriteLine(" --- Producer đã sẵn sàng ---");
Console.WriteLine($"Gửi tin nhắn tới Exchange: '{ExchangeName}' và Queue: '{QueueName}'");
Console.WriteLine("Gõ tin nhắn và nhấn [Enter] để gửi. Nhấn [Enter] trên dòng trống để thoát.");

// --- 3. VÒNG LẶP GỬI TIN NHẮN (PUBLISH LOOP) ---
while (true)
{
    Console.Write("Input message: ");
    var message = Console.ReadLine();

    if (string.IsNullOrEmpty(message))
        break;

    // Chuyển đổi tin nhắn thành mảng bytes
    var body = Encoding.UTF8.GetBytes(message);

    // Gửi tin nhắn tới Exchange
    await channel.BasicPublishAsync(
        exchange: ExchangeName, 
        routingKey: RoutingKey, // Routing Key phải khớp với Binding Key
        body: body
    );
    Console.WriteLine($" [x] Sent '{message}'");
}

Console.WriteLine("Producer finished.");