using System.Text;
using System.Text.Json;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Backend.Services;

/// <summary>
/// Service chạy nền để lắng nghe tin nhắn từ RabbitMQ và phát tới SignalR Hub
/// </summary>
public class RabbitMQConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "notifications";

    public RabbitMQConsumerService(
        ILogger<RabbitMQConsumerService> logger,
        IHubContext<NotificationHub> hubContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _hubContext = hubContext;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Đợi app khởi động hoàn toàn

        try
        {
            await ConnectAsync();
            await ConsumeMessagesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ Consumer Service");
        }
    }

    /// <summary>
    /// Kết nối tới RabbitMQ
    /// </summary>
    private async Task ConnectAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "47.130.33.106",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Tạo queue nếu chưa tồn tại
            _channel.QueueDeclare(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Connected to RabbitMQ successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    /// <summary>
    /// Lắng nghe và xử lý tin nhắn từ RabbitMQ
    /// </summary>
    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogError("RabbitMQ channel is null");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation($"Received message from RabbitMQ: {message}");

                // Phát tin nhắn tới tất cả client đã kết nối qua SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, stoppingToken);

                // Xác nhận đã xử lý tin nhắn
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message");
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false, // Tắt auto-ack để tự quản lý
            consumer: consumer);

        _logger.LogInformation("RabbitMQ Consumer started. Waiting for messages...");

        // Giữ service chạy
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

