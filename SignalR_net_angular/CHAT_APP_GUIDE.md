# 💬 Hướng dẫn ứng dụng Chat Real-time

## 📋 Tổng quan

Ứng dụng chat real-time được xây dựng với:
- **Backend**: .NET 8 + SignalR + PostgreSQL + RabbitMQ
- **Frontend**: Angular 17 + SignalR Client
- **Database**: PostgreSQL (47.130.33.106:5432)
- **Real-time**: SignalR WebSocket

---

## 🚀 Cài đặt và Chạy

### Backend

1. **Cài đặt dependencies:**
```bash
cd SignalR_net_angular/Backend
dotnet restore
```

2. **Kiểm tra connection string trong `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=47.130.33.106;Port=5432;Database=signalr_db;Username=postgres;Password=123456"
  }
}
```

3. **Chạy ứng dụng:**
```bash
dotnet run
```

Backend sẽ chạy tại: `http://localhost:5000` hoặc `http://localhost:8080` (tùy cấu hình)

### Frontend

1. **Cài đặt dependencies:**
```bash
cd SignalR_net_angular/Frontend
npm install
```

2. **Kiểm tra API URL trong các service files:**
- `auth.service.ts`: `apiUrl = 'http://localhost:8888/api'`
- `chat.service.ts`: `apiUrl = 'http://localhost:8888/api'` và `hubUrl = 'http://localhost:8888/chatHub'`

3. **Chạy ứng dụng:**
```bash
npm start
```

Frontend sẽ chạy tại: `http://localhost:4200`

---

## 📊 Database Schema

### Tables

#### **Users**
- `Id` (int, PK)
- `Username` (string, unique)
- `Email` (string, unique)
- `PasswordHash` (string)
- `DisplayName` (string, nullable)
- `AvatarUrl` (string, nullable)
- `CreatedAt` (datetime)
- `LastSeen` (datetime, nullable)
- `IsOnline` (bool)

#### **Conversations**
- `Id` (int, PK)
- `User1Id` (int, FK → Users)
- `User2Id` (int, FK → Users)
- `CreatedAt` (datetime)
- `LastMessageAt` (datetime, nullable)

#### **Messages**
- `Id` (int, PK)
- `ConversationId` (int, FK → Conversations)
- `SenderId` (int, FK → Users)
- `ReceiverId` (int, FK → Users)
- `Content` (string)
- `SentAt` (datetime)
- `IsRead` (bool)
- `ReadAt` (datetime, nullable)

---

## 🔐 Authentication

### Đăng ký

**Endpoint:** `POST /api/Auth/register`

**Request:**
```json
{
  "username": "user1",
  "email": "user1@example.com",
  "password": "password123",
  "displayName": "User One"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "username": "user1",
    "email": "user1@example.com",
    "displayName": "User One",
    "token": "1:user1:1234567890"
  }
}
```

### Đăng nhập

**Endpoint:** `POST /api/Auth/login`

**Request:**
```json
{
  "username": "user1",
  "password": "password123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "username": "user1",
    "email": "user1@example.com",
    "displayName": "User One",
    "token": "1:user1:1234567890"
  }
}
```

**Lưu ý:** Token được lưu trong localStorage và được gửi kèm trong header `Authorization: Bearer <token>` hoặc query string `?token=<token>` cho SignalR.

---

## 💬 Chat API

### Lấy danh sách Users

**Endpoint:** `GET /api/Messages/users?search=keyword`

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 2,
      "username": "user2",
      "displayName": "User Two",
      "avatarUrl": null,
      "isOnline": true,
      "lastSeen": "2024-01-01T10:00:00Z"
    }
  ]
}
```

### Lấy danh sách Conversations

**Endpoint:** `GET /api/Messages/conversations`

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "otherUserId": 2,
      "otherUsername": "user2",
      "otherDisplayName": "User Two",
      "otherIsOnline": true,
      "lastMessage": {
        "id": 10,
        "content": "Hello!",
        "sentAt": "2024-01-01T10:00:00Z",
        "senderId": 2,
        "senderUsername": "user2",
        "isRead": false
      },
      "lastMessageAt": "2024-01-01T10:00:00Z",
      "unreadCount": 2
    }
  ]
}
```

### Lấy tin nhắn trong Conversation

**Endpoint:** `GET /api/Messages/conversation/{otherUserId}?page=1&pageSize=50`

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10,
      "conversationId": 1,
      "senderId": 2,
      "senderUsername": "user2",
      "senderDisplayName": "User Two",
      "receiverId": 1,
      "receiverUsername": "user1",
      "content": "Hello!",
      "sentAt": "2024-01-01T10:00:00Z",
      "isRead": false
    }
  ]
}
```

### Gửi tin nhắn

**Endpoint:** `POST /api/Messages/send`

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "receiverId": 2,
  "content": "Hi there!"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 11,
    "conversationId": 1,
    "senderId": 1,
    "senderUsername": "user1",
    "receiverId": 2,
    "receiverUsername": "user2",
    "content": "Hi there!",
    "sentAt": "2024-01-01T10:05:00Z",
    "isRead": false
  }
}
```

---

## 🔌 SignalR ChatHub

### Kết nối

**URL:** `http://localhost:8888/chatHub?token=<token>`

**Client (Angular):**
```typescript
this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`http://localhost:8888/chatHub?token=${token}`)
  .withAutomaticReconnect()
  .build();

await this.hubConnection.start();
```

### Events

#### **SendMessage** (Client → Server)
```typescript
await hubConnection.invoke('SendMessage', receiverId, content);
```

#### **ReceiveMessage** (Server → Client)
```typescript
hubConnection.on('ReceiveMessage', (message: MessageDto) => {
  // Nhận tin nhắn mới
});
```

#### **MessageSent** (Server → Client)
```typescript
hubConnection.on('MessageSent', (message: MessageDto) => {
  // Xác nhận tin nhắn đã gửi
});
```

#### **UserOnline** (Server → Client)
```typescript
hubConnection.on('UserOnline', (userId: number) => {
  // User đã online
});
```

#### **UserOffline** (Server → Client)
```typescript
hubConnection.on('UserOffline', (userId: number) => {
  // User đã offline
});
```

#### **MarkAsRead** (Client → Server)
```typescript
await hubConnection.invoke('MarkAsRead', messageId);
```

#### **MessageRead** (Server → Client)
```typescript
hubConnection.on('MessageRead', (messageId: number) => {
  // Tin nhắn đã được đọc
});
```

---

## 🎨 Frontend Components

### LoginComponent

- **Chức năng:**
  - Đăng nhập
  - Đăng ký
  - Lưu token vào localStorage

- **File:** `Frontend/src/app/components/login.component.ts`

### ChatComponent

- **Chức năng:**
  - Hiển thị danh sách conversations
  - Tìm kiếm users
  - Chat real-time với user khác
  - Hiển thị trạng thái online/offline
  - Đánh dấu tin nhắn đã đọc

- **File:** `Frontend/src/app/components/chat.component.ts`

---

## 🔄 Flow hoạt động

### 1. Đăng nhập/Đăng ký

```
User → LoginComponent → AuthService → API /Auth/login
→ Lưu token vào localStorage → Chuyển sang ChatComponent
```

### 2. Gửi tin nhắn

```
ChatComponent → ChatService.sendMessage() → SignalR ChatHub.SendMessage()
→ MessageService.SaveMessage() → Database
→ ChatHub phát tới Receiver qua SignalR
→ Receiver nhận qua event "ReceiveMessage"
```

### 3. Nhận tin nhắn real-time

```
Sender gửi → ChatHub → Database → SignalR push tới Receiver
→ ChatService.messageReceived$ → ChatComponent hiển thị
```

---

## 🐛 Troubleshooting

### Database không tạo được tables

**Giải pháp:**
1. Kiểm tra connection string trong `appsettings.json`
2. Đảm bảo PostgreSQL đang chạy
3. Database sẽ tự động tạo khi chạy `dotnet run` (Development mode)

### SignalR không kết nối được

**Kiểm tra:**
1. Backend đang chạy tại đúng port
2. Token được gửi đúng trong query string
3. CORS đã được cấu hình đúng
4. Kiểm tra console browser để xem lỗi cụ thể

### Tin nhắn không hiển thị real-time

**Kiểm tra:**
1. SignalR connection đã thành công chưa
2. Event handler đã đăng ký chưa (`messageReceived$`)
3. Kiểm tra Network tab để xem WebSocket connection

---

## 📝 Notes

- Token authentication đơn giản (có thể nâng cấp lên JWT sau)
- Password được hash bằng SHA256 (có thể nâng cấp lên BCrypt)
- Database tự động tạo trong Development mode
- SignalR tự động reconnect nếu mất kết nối

---

**Chúc bạn sử dụng ứng dụng chat vui vẻ! 💬**

