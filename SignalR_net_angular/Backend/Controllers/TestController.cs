using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly IConfiguration _configuration;

    public TestController(ILogger<TestController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// API để test gửi tin nhắn vào RabbitMQ (sẽ được consumer xử lý và phát qua SignalR)
    /// </summary>
    [HttpPost("send-message")]
    public IActionResult SendMessage([FromBody] string message)
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

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: "notifications",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(
                exchange: "",
                routingKey: "notifications",
                basicProperties: null,
                body: body);

            _logger.LogInformation($"Message sent to RabbitMQ: {message}");

            return Ok(new { success = true, message = "Message sent to RabbitMQ" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to RabbitMQ");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

