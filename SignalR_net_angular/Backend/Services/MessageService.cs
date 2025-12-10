using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class MessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ApplicationDbContext context, ILogger<MessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lấy hoặc tạo conversation giữa 2 users
    /// </summary>
    public async Task<Conversation> GetOrCreateConversationAsync(int user1Id, int user2Id)
    {
        // Đảm bảo user1Id < user2Id để tránh duplicate
        var (minId, maxId) = user1Id < user2Id ? (user1Id, user2Id) : (user2Id, user1Id);

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => 
                (c.User1Id == minId && c.User2Id == maxId) ||
                (c.User1Id == maxId && c.User2Id == minId));

        if (conversation == null)
        {
            conversation = new Conversation
            {
                User1Id = minId,
                User2Id = maxId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        return conversation;
    }

    /// <summary>
    /// Gửi tin nhắn
    /// </summary>
    public async Task<MessageDto> SendMessageAsync(int senderId, int receiverId, string content)
    {
        // Lấy hoặc tạo conversation
        var conversation = await GetOrCreateConversationAsync(senderId, receiverId);

        // Tạo message
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);

        // Cập nhật LastMessageAt của conversation
        conversation.LastMessageAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Message sent from {senderId} to {receiverId}");

        // Map to DTO
        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(receiverId);

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderUsername = sender?.Username ?? "",
            SenderDisplayName = sender?.DisplayName,
            ReceiverId = message.ReceiverId,
            ReceiverUsername = receiver?.Username ?? "",
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = message.IsRead
        };
    }

    /// <summary>
    /// Lấy danh sách tin nhắn trong conversation
    /// </summary>
    public async Task<List<MessageDto>> GetMessagesAsync(int userId, int otherUserId, int page = 1, int pageSize = 50)
    {
        var conversation = await GetOrCreateConversationAsync(userId, otherUserId);

        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt) // Đảo lại để hiển thị từ cũ đến mới
            .ToListAsync();

        return messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderUsername = m.Sender.Username,
            SenderDisplayName = m.Sender.DisplayName,
            ReceiverId = m.ReceiverId,
            ReceiverUsername = m.Receiver.Username,
            Content = m.Content,
            SentAt = m.SentAt,
            IsRead = m.IsRead
        }).ToList();
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);

        if (message != null && !message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Lấy danh sách conversations của user
    /// </summary>
    public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
    {
        var conversations = await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var result = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            var otherUser = conv.User1Id == userId ? conv.User2 : conv.User1;
            var lastMessage = conv.Messages.FirstOrDefault();

            result.Add(new ConversationDto
            {
                Id = conv.Id,
                OtherUserId = otherUser.Id,
                OtherUsername = otherUser.Username,
                OtherDisplayName = otherUser.DisplayName,
                OtherAvatarUrl = otherUser.AvatarUrl,
                OtherIsOnline = otherUser.IsOnline,
                LastMessage = lastMessage != null ? new MessageDto
                {
                    Id = lastMessage.Id,
                    Content = lastMessage.Content,
                    SentAt = lastMessage.SentAt,
                    SenderId = lastMessage.SenderId,
                    SenderUsername = lastMessage.Sender.Username,
                    IsRead = lastMessage.IsRead
                } : null,
                LastMessageAt = conv.LastMessageAt ?? conv.CreatedAt,
                UnreadCount = await _context.Messages
                    .CountAsync(m => m.ConversationId == conv.Id && 
                                    m.ReceiverId == userId && 
                                    !m.IsRead)
            });
        }

        return result;
    }

    /// <summary>
    /// Lấy message theo ID
    /// </summary>
    public async Task<MessageDto?> GetMessageByIdAsync(int messageId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) return null;

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderUsername = message.Sender.Username,
            SenderDisplayName = message.Sender.DisplayName,
            ReceiverId = message.ReceiverId,
            ReceiverUsername = message.Receiver.Username,
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = message.IsRead
        };
    }

    /// <summary>
    /// Lấy danh sách users (để chọn người chat)
    /// </summary>
    public async Task<List<UserDto>> GetUsersAsync(int currentUserId, string? search = null)
    {
        var query = _context.Users.Where(u => u.Id != currentUserId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.Username.Contains(search) || 
                (u.DisplayName != null && u.DisplayName.Contains(search)));
        }

        var users = await query
            .OrderBy(u => u.Username)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.DisplayName,
            AvatarUrl = u.AvatarUrl,
            IsOnline = u.IsOnline,
            LastSeen = u.LastSeen
        }).ToList();
    }
}

public class ConversationDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUsername { get; set; } = string.Empty;
    public string? OtherDisplayName { get; set; }
    public string? OtherAvatarUrl { get; set; }
    public bool OtherIsOnline { get; set; }
    public MessageDto? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

