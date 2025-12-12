using Backend.DTOs;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Backend.Services;

public class MongoChatOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = "signalr_chat";
    public string Collection { get; set; } = "messages";
}

public class MongoChatDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>
/// Lưu tin nhắn sang MongoDB để phục vụ view lịch sử nhanh
/// </summary>
public class MongoChatService
{
    private readonly IMongoCollection<MongoChatDocument> _collection;
    private readonly ILogger<MongoChatService> _logger;

    public MongoChatService(IOptions<MongoChatOptions> options, ILogger<MongoChatService> logger)
    {
        _logger = logger;
        var client = new MongoClient(options.Value.ConnectionString);
        var db = client.GetDatabase(options.Value.Database);
        _collection = db.GetCollection<MongoChatDocument>(options.Value.Collection);
    }

    private int ComputeConversationId(int user1, int user2)
    {
        var min = Math.Min(user1, user2);
        var max = Math.Max(user1, user2);
        // Ghép thành một số duy nhất, tránh trùng (giả sử userId < 1.000.000)
        return (min * 1_000_000) + max;
    }

    public async Task<MessageDto> SendMessageAsync(int senderId, int receiverId, string content)
    {
        var convId = ComputeConversationId(senderId, receiverId);
        var now = DateTime.UtcNow;
        var doc = new MongoChatDocument
        {
            ConversationId = convId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = now,
            IsRead = false
        };

        await _collection.InsertOneAsync(doc);

        return new MessageDto
        {
            Id = 0,
            ConversationId = convId,
            SenderId = senderId,
            SenderUsername = senderId.ToString(),
            ReceiverId = receiverId,
            ReceiverUsername = receiverId.ToString(),
            Content = content,
            SentAt = now,
            IsRead = false
        };
    }

    public async Task SaveMessageAsync(MessageDto message)
    {
        try
        {
            var doc = new MongoChatDocument
            {
                ConversationId = message.ConversationId == 0
                    ? ComputeConversationId(message.SenderId, message.ReceiverId)
                    : message.ConversationId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };
            await _collection.InsertOneAsync(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mongo save message failed");
        }
    }

    public async Task<List<MessageDto>> GetHistoryAsync(int userId, int otherUserId, int limit = 100)
    {
        var convId = ComputeConversationId(userId, otherUserId);
        var filter = Builders<MongoChatDocument>.Filter.Eq(x => x.ConversationId, convId);

        var docs = await _collection.Find(filter)
            .SortByDescending(x => x.SentAt)
            .Limit(limit)
            .ToListAsync();

        // Map sang DTO (không cần username ở đây)
        return docs
            .OrderBy(x => x.SentAt)
            .Select(x => new MessageDto
            {
                Id = 0,
                ConversationId = x.ConversationId,
                SenderId = x.SenderId,
                SenderUsername = x.SenderId.ToString(),
                ReceiverId = x.ReceiverId,
                ReceiverUsername = x.ReceiverId.ToString(),
                Content = x.Content,
                SentAt = x.SentAt,
                IsRead = x.IsRead
            })
            .ToList();
    }
}

