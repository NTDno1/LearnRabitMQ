using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Hubs;

/// <summary>
/// SignalR Hub cho ứng dụng chat real-time
/// </summary>
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IServiceProvider _serviceProvider;
    private static readonly Dictionary<int, string> _userConnections = new();

    public ChatHub(
        ILogger<ChatHub> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private MessageService GetMessageService()
    {
        return _serviceProvider.GetRequiredService<MessageService>();
    }

    private AuthService GetAuthService()
    {
        return _serviceProvider.GetRequiredService<AuthService>();
    }

    private MongoChatService GetMongoChatService()
    {
        return _serviceProvider.GetRequiredService<MongoChatService>();
    }

    /// <summary>
    /// Khi client kết nối - xác thực và lưu connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            // Lấy token từ query string hoặc header
            var token = Context.GetHttpContext()?.Request.Query["token"].ToString() 
                       ?? Context.GetHttpContext()?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                Context.Abort();
                return;
            }

            // Xác thực user
            var authService = GetAuthService();
            var user = await authService.ValidateTokenAsync(token);
            if (user == null)
            {
                Context.Abort();
                return;
            }

            // Lưu connection mapping
            _userConnections[user.Id] = Context.ConnectionId;

            // Thêm vào group của user (để dễ dàng gửi message tới user cụ thể)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");

            // Cập nhật trạng thái online
            await Clients.All.SendAsync("UserOnline", user.Id);

            _logger.LogInformation($"User {user.Username} (ID: {user.Id}) connected. ConnectionId: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
            Context.Abort();
        }
    }

    /// <summary>
    /// Khi client ngắt kết nối
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId > 0)
            {
                _userConnections.Remove(userId);
                await Clients.All.SendAsync("UserOffline", userId);
                _logger.LogInformation($"User {userId} disconnected");
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync");
        }
    }

    /// <summary>
    /// Gửi tin nhắn tới user khác
    /// </summary>
    public async Task SendMessage(int receiverId, string content)
    {
        try
        {
            // Lấy sender từ connection
            var senderId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (senderId == 0)
            {
                await Clients.Caller.SendAsync("Error", "User not authenticated");
                return;
            }

            // Lưu message vào database (SQL)
            var messageService = GetMessageService();
            var messageDto = await messageService.SendMessageAsync(senderId, receiverId, content);

            // Gửi message tới receiver (nếu đang online)
            if (_userConnections.ContainsKey(receiverId))
            {
                await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", messageDto);
            }

            // Gửi lại cho sender để confirm
            await Clients.Caller.SendAsync("MessageSent", messageDto);

            _logger.LogInformation($"Message sent from {senderId} to {receiverId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task MarkAsRead(int messageId)
    {
        try
        {
            var userId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId == 0) return;

            var messageService = GetMessageService();
            await messageService.MarkAsReadAsync(messageId, userId);

            // Thông báo cho sender rằng message đã được đọc
            var message = await messageService.GetMessageByIdAsync(messageId);
            if (message != null && _userConnections.ContainsKey(message.SenderId))
            {
                await Clients.Group($"user_{message.SenderId}").SendAsync("MessageRead", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read");
        }
    }

    /// <summary>
    /// Lấy connection ID của user
    /// </summary>
    public static string? GetConnectionId(int userId)
    {
        return _userConnections.TryGetValue(userId, out var connectionId) ? connectionId : null;
    }
}

