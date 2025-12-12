using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly MessageService _messageService;
    private readonly AuthService _authService;
        private readonly MongoChatService _mongoChatService;
        private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        MessageService messageService,
        AuthService authService,
            MongoChatService mongoChatService,
            IHubContext<ChatHub> hubContext,
        ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _authService = authService;
            _mongoChatService = mongoChatService;
            _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Lấy user hiện tại từ token
    /// </summary>
    private async Task<int?> GetCurrentUserIdAsync()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
        {
            token = Request.Query["token"].ToString();
        }

        if (string.IsNullOrEmpty(token))
            return null;

        var user = await _authService.ValidateTokenAsync(token);
        return user?.Id;
    }

    /// <summary>
    /// Gửi tin nhắn
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        if (string.IsNullOrEmpty(request.Content))
        {
            return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống" });
        }

        try
        {
            var message = await _messageService.SendMessageAsync(userId.Value, request.ReceiverId, request.Content);
            return Ok(new { success = true, data = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { success = false, message = "Lỗi khi gửi tin nhắn" });
        }
    }

    /// <summary>
    /// Lấy danh sách tin nhắn với user khác
    /// </summary>
    [HttpGet("conversation/{otherUserId}")]
    public async Task<IActionResult> GetMessages(int otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        try
        {
            var messages = await _messageService.GetMessagesAsync(userId.Value, otherUserId, page, pageSize);
            return Ok(new { success = true, data = messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages");
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy tin nhắn" });
        }
    }

    /// <summary>
    /// Lấy danh sách conversations
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        try
        {
            var conversations = await _messageService.GetConversationsAsync(userId.Value);
            return Ok(new { success = true, data = conversations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy danh sách cuộc trò chuyện" });
        }
    }

    /// <summary>
    /// Lấy lịch sử chat lưu trên MongoDB
    /// </summary>
    [HttpGet("mongo/history/{otherUserId}")]
    public async Task<IActionResult> GetMongoHistory(int otherUserId, [FromQuery] int limit = 100)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        try
        {
            var messages = await _mongoChatService.GetHistoryAsync(userId.Value, otherUserId, limit);
            return Ok(new { success = true, data = messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mongo history");
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy lịch sử (Mongo)" });
        }
    }

    /// <summary>
    /// Gửi tin nhắn và lưu trực tiếp MongoDB (kênh Mongo)
    /// </summary>
    [HttpPost("mongo/send")]
    public async Task<IActionResult> SendMongoMessage([FromBody] SendMessageRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        if (string.IsNullOrEmpty(request.Content))
        {
            return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống" });
        }

        try
        {
            var message = await _mongoChatService.SendMessageAsync(userId.Value, request.ReceiverId, request.Content);

            // Push realtime qua SignalR (channel Mongo)
            await _hubContext.Clients.Group($"user_{request.ReceiverId}")
                .SendAsync("ReceiveMongoMessage", message);
            await _hubContext.Clients.Group($"user_{userId.Value}")
                .SendAsync("MongoMessageSent", message);

            return Ok(new { success = true, data = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mongo message");
            return StatusCode(500, new { success = false, message = "Lỗi khi gửi tin nhắn (Mongo)" });
        }
    }
    /// <summary>
    /// Lấy danh sách users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        try
        {
            var users = await _messageService.GetUsersAsync(userId.Value, search);
            return Ok(new { success = true, data = users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy danh sách users" });
        }
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    [HttpPost("read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
        }

        try
        {
            await _messageService.MarkAsReadAsync(messageId, userId.Value);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read");
            return StatusCode(500, new { success = false, message = "Lỗi khi đánh dấu đã đọc" });
        }
    }
}

