# 📖 Luồng Chạy Chi Tiết - Dự Án Chat Real-time

Tài liệu này mô tả **chi tiết từng hàm, tham số, logic xử lý** và **thứ tự chạy** của toàn bộ hệ thống Frontend và Backend.

---

## 🏗️ Cấu trúc Tổng quan

```
Frontend (Angular 17)
├── app.component.ts          → Component gốc, quyết định hiển thị Login hay Chat
├── login.component.ts         → Form đăng nhập/đăng ký
├── chat.component.ts          → Giao diện chat (2 view: SQL/SignalR và Mongo)
├── services/
│   ├── auth.service.ts        → Quản lý authentication
│   └── chat.service.ts        → Quản lý chat API và SignalR

Backend (.NET 8)
├── Program.cs                 → Khởi tạo ứng dụng, đăng ký services
├── Controllers/
│   ├── AuthController.cs      → API đăng nhập/đăng ký
│   └── MessagesController.cs  → API chat (SQL và Mongo)
├── Services/
│   ├── AuthService.cs         → Logic authentication
│   ├── MessageService.cs      → Logic chat với PostgreSQL
│   └── MongoChatService.cs    → Logic chat với MongoDB
└── Hubs/
    └── ChatHub.cs             → SignalR Hub cho real-time
```

---

## 🚀 PHẦN 1: KHỞI ĐỘNG HỆ THỐNG

### Backend: Program.cs

#### **Hàm: `Main()` (entry point)**

**Tham số:** Không có (entry point)

**Logic xử lý:**

1. **Tạo WebApplicationBuilder:**
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   ```
   - Đọc `appsettings.json`
   - Khởi tạo DI container

2. **Đăng ký Controllers và Swagger:**
   ```csharp
   builder.Services.AddControllers();
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen(c => { ... });
   ```
   - Cấu hình Swagger với Bearer token authentication

3. **Đăng ký DbContext (PostgreSQL):**
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
   ```
   - Connection string: `Host=47.130.33.106;Port=5432;Database=signalr_db;Username=postgres;Password=123456`

4. **Đăng ký Services:**
   ```csharp
   builder.Services.AddScoped<AuthService>();
   builder.Services.AddScoped<MessageService>();
   ```
   - `AuthService`: Xử lý đăng nhập/đăng ký
   - `MessageService`: Xử lý chat với PostgreSQL

5. **Đăng ký MongoDB:**
   ```csharp
   builder.Services.Configure<MongoChatOptions>(builder.Configuration.GetSection("MongoDb"));
   builder.Services.AddSingleton<IMongoClient>(sp => { ... });
   builder.Services.AddSingleton<MongoChatService>();
   ```
   - Connection string: `mongodb+srv://datt19112001_db_user:1@mongodbdatnt.bc8xywz.mongodb.net/...`
   - Database: `signalr_chat`, Collection: `messages`

6. **Cấu hình CORS:**
   ```csharp
   builder.Services.AddCors(options => {
       options.AddPolicy("AllowAngular", policy => {
           policy.SetIsOriginAllowed(origin => true)
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
       });
   });
   ```

7. **Đăng ký SignalR:**
   ```csharp
   builder.Services.AddSignalR();
   ```

8. **Đăng ký Background Service:**
   ```csharp
   builder.Services.AddHostedService<RabbitMQConsumerService>();
   ```

9. **Build và Configure Pipeline:**
   ```csharp
   var app = builder.Build();
   app.UseSwagger();
   app.UseSwaggerUI();
   app.UseCors("AllowAngular");
   app.MapControllers();
   app.MapHub<NotificationHub>("/notificationHub");
   app.MapHub<ChatHub>("/chatHub");
   ```

10. **Tự động tạo Database (Development):**
    ```csharp
    if (app.Environment.IsDevelopment()) {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
    }
    ```

11. **Chạy ứng dụng:**
    ```csharp
    app.Run();
    ```
    - Backend chạy tại: `https://localhost:5001`

---

### Frontend: main.ts

#### **Hàm: `bootstrapApplication()`**

**Tham số:**
- `AppComponent`: Component gốc
- `providers`: `[provideHttpClient()]` - Cung cấp HttpClient cho toàn app

**Logic xử lý:**
- Khởi tạo Angular application
- Load `AppComponent` làm root component

---

### Frontend: app.component.ts

#### **Class: `AppComponent`**

**Constructor:**
```typescript
constructor(public authService: AuthService) {}
```
- Inject `AuthService` để kiểm tra authentication

**Template Logic (app.component.html):**
```html
<app-login *ngIf="!authService.isAuthenticated()"></app-login>
<app-chat *ngIf="authService.isAuthenticated()"></app-chat>
```
- Nếu chưa đăng nhập → Hiển thị `LoginComponent`
- Nếu đã đăng nhập → Hiển thị `ChatComponent`

---

## 🔐 PHẦN 2: ĐĂNG NHẬP / ĐĂNG KÝ

### Frontend: login.component.ts

#### **Hàm: `onSubmit()`**

**Tham số:** Không có (gọi từ form submit)

**Logic xử lý:**

1. **Validate input:**
   ```typescript
   if (!this.username || !this.password) {
       this.errorMessage = 'Vui lòng điền đầy đủ thông tin';
       return;
   }
   if (!this.isLoginMode && !this.email) {
       this.errorMessage = 'Vui lòng nhập email';
       return;
   }
   ```

2. **Gọi AuthService:**
   ```typescript
   if (this.isLoginMode) {
       await this.authService.login({
           username: this.username,
           password: this.password
       }).toPromise();
   } else {
       await this.authService.register({
           username: this.username,
           email: this.email,
           password: this.password,
           displayName: this.displayName || undefined
       }).toPromise();
   }
   ```

3. **Reload page:**
   ```typescript
   window.location.reload();
   ```
   - Để `AppComponent` detect user mới và chuyển sang `ChatComponent`

---

### Frontend: auth.service.ts

#### **Hàm: `login(request: LoginRequest)`**

**Tham số:**
- `request: LoginRequest`
  - `username: string`
  - `password: string`

**Logic xử lý:**

1. **Gọi API:**
   ```typescript
   return this.http.post<any>(`${this.apiUrl}/Auth/login`, request)
   ```
   - URL: `https://localhost:5001/api/Auth/login`
   - Method: `POST`
   - Body: `{ username, password }`

2. **Xử lý response:**
   ```typescript
   .pipe(
       tap(response => {
           if (response.success && response.data) {
               this.setCurrentUser(response.data);
           }
       })
   )
   ```
   - Nếu thành công → Gọi `setCurrentUser()`

3. **Lưu user:**
   ```typescript
   private setCurrentUser(user: AuthResponse): void {
       localStorage.setItem('currentUser', JSON.stringify(user));
       this.currentUserSubject.next(user);
   }
   ```
   - Lưu vào `localStorage`
   - Emit qua `BehaviorSubject` để các component khác subscribe

**Return:** `Observable<any>`

---

#### **Hàm: `register(request: RegisterRequest)`**

**Tham số:**
- `request: RegisterRequest`
  - `username: string`
  - `email: string`
  - `password: string`
  - `displayName?: string`

**Logic xử lý:** Tương tự `login()`, nhưng gọi `/Auth/register`

---

### Backend: AuthController.cs

#### **Hàm: `Login([FromBody] LoginRequest request)`**

**Tham số:**
- `request: LoginRequest`
  - `Username: string`
  - `Password: string`

**Logic xử lý:**

1. **Validate:**
   ```csharp
   if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
   {
       return BadRequest(new { success = false, message = "Username và Password là bắt buộc" });
   }
   ```

2. **Gọi AuthService:**
   ```csharp
   var result = await _authService.LoginAsync(request);
   ```

3. **Xử lý kết quả:**
   ```csharp
   if (result == null)
   {
       return Unauthorized(new { success = false, message = "Username hoặc Password không đúng" });
   }
   return Ok(new { success = true, data = result });
   ```

**Return:** `IActionResult`

---

### Backend: AuthService.cs

#### **Hàm: `LoginAsync(LoginRequest request)`**

**Tham số:**
- `request: LoginRequest`
  - `Username: string`
  - `Password: string`

**Logic xử lý:**

1. **Tìm user trong database:**
   ```csharp
   var user = await _context.Users
       .FirstOrDefaultAsync(u => u.Username == request.Username);
   ```
   - Query PostgreSQL: `SELECT * FROM Users WHERE Username = @username`

2. **Kiểm tra user tồn tại:**
   ```csharp
   if (user == null)
   {
       return null; // User không tồn tại
   }
   ```

3. **Verify password:**
   ```csharp
   if (!VerifyPassword(request.Password, user.PasswordHash))
   {
       return null; // Password sai
   }
   ```
   - Gọi `HashPassword(request.Password)` → So sánh với `user.PasswordHash`

4. **Cập nhật trạng thái online:**
   ```csharp
   user.IsOnline = true;
   user.LastSeen = DateTime.UtcNow;
   await _context.SaveChangesAsync();
   ```
   - Update PostgreSQL: `UPDATE Users SET IsOnline = true, LastSeen = NOW() WHERE Id = @id`

5. **Tạo token:**
   ```csharp
   var token = GenerateSimpleToken(user.Id, user.Username);
   ```
   - Format: `{userId}:{username}:{timestamp}`
   - Ví dụ: `1:dat1:639009554407193211`

6. **Trả về AuthResponse:**
   ```csharp
   return new AuthResponse
   {
       UserId = user.Id,
       Username = user.Username,
       Email = user.Email,
       DisplayName = user.DisplayName,
       Token = token
   };
   ```

**Return:** `Task<AuthResponse?>`

---

#### **Hàm: `HashPassword(string password)`**

**Tham số:**
- `password: string` - Password gốc

**Logic xử lý:**

1. **Tạo SHA256 hash:**
   ```csharp
   using var sha256 = SHA256.Create();
   var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
   ```

2. **Convert sang Base64:**
   ```csharp
   return Convert.ToBase64String(hashedBytes);
   ```

**Return:** `string` - Hash password

**Ví dụ:**
- Input: `"123"`
- Output: `"pmWkWSBCL51Bfkhn79xPuKBKHz//H6J+mNynAa7aU2Y="`

---

#### **Hàm: `VerifyPassword(string password, string hash)`**

**Tham số:**
- `password: string` - Password cần verify
- `hash: string` - Hash đã lưu trong database

**Logic xử lý:**

1. **Hash password mới:**
   ```csharp
   var passwordHash = HashPassword(password);
   ```

2. **So sánh:**
   ```csharp
   return passwordHash == hash;
   ```

**Return:** `bool` - `true` nếu khớp

---

#### **Hàm: `GenerateSimpleToken(int userId, string username)`**

**Tham số:**
- `userId: int` - ID của user
- `username: string` - Username

**Logic xử lý:**

1. **Lấy timestamp:**
   ```csharp
   var timestamp = DateTime.UtcNow.Ticks;
   ```

2. **Ghép thành token:**
   ```csharp
   return $"{userId}:{username}:{timestamp}";
   ```

**Return:** `string` - Token

**Ví dụ:** `"1:dat1:639009554407193211"`

---

#### **Hàm: `ValidateTokenAsync(string token)`**

**Tham số:**
- `token: string` - Token cần validate (format: `userId:username:timestamp`)

**Logic xử lý:**

1. **Parse token:**
   ```csharp
   var parts = token.Split(':');
   if (parts.Length < 2)
       return null;
   var userId = int.Parse(parts[0]);
   var username = parts[1];
   ```

2. **Tìm user trong database:**
   ```csharp
   var user = await _context.Users.FindAsync(userId);
   ```

3. **Kiểm tra username khớp:**
   ```csharp
   if (user == null || user.Username != username)
       return null;
   ```

4. **Trả về user:**
   ```csharp
   return user;
   ```

**Return:** `Task<User?>` - User nếu hợp lệ, `null` nếu không

---

## 💬 PHẦN 3: CHAT SQL/SIGNALR

### Frontend: chat.component.ts

#### **Hàm: `ngOnInit()`**

**Tham số:** Không có (lifecycle hook)

**Logic xử lý:**

1. **Lấy current user:**
   ```typescript
   this.currentUser = this.authService.getCurrentUser();
   if (!this.currentUser) {
       return;
   }
   ```

2. **Kết nối SignalR:**
   ```typescript
   try {
       await this.chatService.startConnection();
       this.setupSignalRHandlers();
   } catch (error) {
       console.error('Lỗi kết nối SignalR:', error);
   }
   ```

3. **Load dữ liệu:**
   ```typescript
   await this.loadConversations();
   await this.loadUsers();
   ```

---

### Frontend: chat.service.ts

#### **Hàm: `startConnection()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Lấy token:**
   ```typescript
   const token = this.authService.getToken();
   if (!token) {
       return Promise.reject('Chưa đăng nhập');
   }
   ```

2. **Tạo Hub Connection:**
   ```typescript
   const hubUrl = `${environment.chatHubUrl}?token=${encodeURIComponent(token)}`;
   // hubUrl = "https://localhost:5001/chatHub?token=1:dat1:639009554407193211"
   
   this.hubConnection = new signalR.HubConnectionBuilder()
       .withUrl(hubUrl)
       .withAutomaticReconnect()
       .build();
   ```

3. **Đăng ký event handlers:**
   ```typescript
   this.registerHandlers();
   ```

4. **Bắt đầu kết nối:**
   ```typescript
   return this.hubConnection.start();
   ```
   - Tạo WebSocket connection tới backend
   - Backend sẽ gọi `ChatHub.OnConnectedAsync()`

**Return:** `Promise<void>`

---

### Backend: ChatHub.cs

#### **Hàm: `OnConnectedAsync()`**

**Tham số:** Không có (override từ `Hub`)

**Logic xử lý:**

1. **Lấy token từ query string:**
   ```csharp
   var token = Context.GetHttpContext()?.Request.Query["token"].ToString()
              ?? Context.GetHttpContext()?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
   ```
   - Token được gửi trong URL: `?token=1:dat1:639009554407193211`

2. **Validate token:**
   ```csharp
   if (string.IsNullOrEmpty(token))
   {
       Context.Abort();
       return;
   }
   var authService = GetAuthService();
   var user = await authService.ValidateTokenAsync(token);
   if (user == null)
   {
       Context.Abort();
       return;
   }
   ```
   - Gọi `AuthService.ValidateTokenAsync()` → Parse token → Tìm user trong DB

3. **Lưu connection mapping:**
   ```csharp
   _userConnections[user.Id] = Context.ConnectionId;
   ```
   - Dictionary: `{ userId: connectionId }`
   - Ví dụ: `{ 1: "abc123xyz" }`

4. **Thêm vào group:**
   ```csharp
   await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");
   ```
   - Group name: `"user_1"` (cho user ID = 1)
   - Dùng để gửi message tới user cụ thể

5. **Broadcast user online:**
   ```csharp
   await Clients.All.SendAsync("UserOnline", user.Id);
   ```
   - Gửi event `"UserOnline"` với `userId` tới tất cả clients

6. **Log:**
   ```csharp
   _logger.LogInformation($"User {user.Username} (ID: {user.Id}) connected. ConnectionId: {Context.ConnectionId}");
   ```

**Return:** `Task`

---

### Frontend: chat.service.ts

#### **Hàm: `registerHandlers()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Đăng ký event "ReceiveMessage":**
   ```typescript
   this.hubConnection.on('ReceiveMessage', (message: MessageDto) => {
       this.messageReceivedSubject.next(message);
   });
   ```
   - Khi nhận tin nhắn từ người khác → Emit qua `messageReceived$`

2. **Đăng ký event "MessageSent":**
   ```typescript
   this.hubConnection.on('MessageSent', (message: MessageDto) => {
       this.messageSentSubject.next(message);
   });
   ```
   - Khi gửi tin nhắn thành công → Emit qua `messageSent$`

3. **Đăng ký event "UserOnline":**
   ```typescript
   this.hubConnection.on('UserOnline', (userId: number) => {
       this.userOnlineSubject.next(userId);
   });
   ```

4. **Đăng ký event "UserOffline":**
   ```typescript
   this.hubConnection.on('UserOffline', (userId: number) => {
       this.userOfflineSubject.next(userId);
   });
   ```

5. **Đăng ký event "ReceiveMongoMessage":**
   ```typescript
   this.hubConnection.on('ReceiveMongoMessage', (message: MessageDto) => {
       this.mongoMessageReceivedSubject.next(message);
   });
   ```

6. **Đăng ký event "MongoMessageSent":**
   ```typescript
   this.hubConnection.on('MongoMessageSent', (message: MessageDto) => {
       this.mongoMessageSentSubject.next(message);
   });
   ```

---

### Frontend: chat.component.ts

#### **Hàm: `setupSignalRHandlers()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Subscribe "messageReceived$":**
   ```typescript
   this.chatService.messageReceived$.subscribe(message => {
       if (this.selectedConversation && 
           (message.senderId === this.selectedConversation.otherUserId || 
            message.receiverId === this.selectedConversation.otherUserId)) {
           this.messages.push(message);
           this.scrollToBottom();
       }
       this.loadConversations(); // Reload để cập nhật last message
   });
   ```
   - Nếu đang xem conversation với người gửi/nhận → Thêm message vào list
   - Reload conversations để cập nhật last message

2. **Subscribe "messageSent$":**
   ```typescript
   this.chatService.messageSent$.subscribe(message => {
       if (this.selectedConversation &&
           (message.receiverId === this.selectedConversation.otherUserId || 
            message.senderId === this.selectedConversation.otherUserId)) {
           if (!this.existsMessage(this.messages, message)) {
               this.messages.push(message);
               this.scrollToBottom();
           }
       }
   });
   ```
   - Khi gửi tin nhắn thành công → Hiển thị ngay (không cần reload)
   - Kiểm tra trùng để tránh hiển thị 2 lần

3. **Subscribe "userOnline$" và "userOffline$":**
   ```typescript
   this.chatService.userOnline$.subscribe(userId => {
       const user = this.users.find(u => u.id === userId);
       if (user) {
           user.isOnline = true;
       }
   });
   ```

---

### Frontend: chat.component.ts

#### **Hàm: `sendMessage()`**

**Tham số:** Không có (gọi từ button click hoặc Enter)

**Logic xử lý:**

1. **Validate:**
   ```typescript
   if (!this.newMessage.trim() || !this.selectedConversation) {
       return;
   }
   ```

2. **Lấy content và clear input:**
   ```typescript
   const content = this.newMessage.trim();
   this.newMessage = '';
   ```

3. **Gửi qua SignalR:**
   ```typescript
   try {
       await this.chatService.sendMessage(this.selectedConversation.otherUserId, content);
       // Message sẽ được thêm vào list qua SignalR event "MessageSent"
   } catch (error) {
       // Fallback: gửi qua API
       const response = await this.chatService.sendMessageViaApi(
           this.selectedConversation.otherUserId,
           content
       ).toPromise();
       if (response?.success) {
           this.messages.push(response.data);
           this.scrollToBottom();
       }
   }
   ```

---

### Frontend: chat.service.ts

#### **Hàm: `sendMessage(receiverId: number, content: string)`**

**Tham số:**
- `receiverId: number` - ID người nhận
- `content: string` - Nội dung tin nhắn

**Logic xử lý:**

1. **Kiểm tra connection:**
   ```typescript
   if (!this.hubConnection) {
       return Promise.reject('Chưa kết nối tới ChatHub');
   }
   ```

2. **Gọi SignalR method:**
   ```typescript
   return this.hubConnection.invoke('SendMessage', receiverId, content);
   ```
   - Gọi method `SendMessage` trên `ChatHub`
   - Backend sẽ xử lý trong `ChatHub.SendMessage()`

**Return:** `Promise<void>`

---

### Backend: ChatHub.cs

#### **Hàm: `SendMessage(int receiverId, string content)`**

**Tham số:**
- `receiverId: int` - ID người nhận
- `content: string` - Nội dung tin nhắn

**Logic xử lý:**

1. **Lấy sender từ connection:**
   ```csharp
   var senderId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
   if (senderId == 0)
   {
       await Clients.Caller.SendAsync("Error", "User not authenticated");
       return;
   }
   ```
   - Tìm `userId` từ `connectionId` trong dictionary `_userConnections`

2. **Lưu message vào database (SQL):**
   ```csharp
   var messageService = GetMessageService();
   var messageDto = await messageService.SendMessageAsync(senderId, receiverId, content);
   ```
   - Gọi `MessageService.SendMessageAsync()` → Lưu vào PostgreSQL

3. **Gửi message tới receiver (nếu đang online):**
   ```csharp
   if (_userConnections.ContainsKey(receiverId))
   {
       await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", messageDto);
   }
   ```
   - Kiểm tra receiver có đang online không
   - Gửi event `"ReceiveMessage"` tới group `"user_{receiverId}"`

4. **Gửi lại cho sender để confirm:**
   ```csharp
   await Clients.Caller.SendAsync("MessageSent", messageDto);
   ```
   - Gửi event `"MessageSent"` tới sender (người gửi)

**Return:** `Task`

---

### Backend: MessageService.cs

#### **Hàm: `SendMessageAsync(int senderId, int receiverId, string content)`**

**Tham số:**
- `senderId: int` - ID người gửi
- `receiverId: int` - ID người nhận
- `content: string` - Nội dung tin nhắn

**Logic xử lý:**

1. **Lấy hoặc tạo conversation:**
   ```csharp
   var conversation = await GetOrCreateConversationAsync(senderId, receiverId);
   ```
   - Gọi `GetOrCreateConversationAsync()` → Tìm hoặc tạo conversation giữa 2 users

2. **Tạo message entity:**
   ```csharp
   var message = new Message
   {
       ConversationId = conversation.Id,
       SenderId = senderId,
       ReceiverId = receiverId,
       Content = content,
       SentAt = DateTime.UtcNow,
       IsRead = false
   };
   ```

3. **Lưu vào database:**
   ```csharp
   _context.Messages.Add(message);
   conversation.LastMessageAt = DateTime.UtcNow;
   await _context.SaveChangesAsync();
   ```
   - INSERT vào bảng `Messages`
   - UPDATE `LastMessageAt` của `Conversations`

4. **Lấy thông tin sender và receiver:**
   ```csharp
   var sender = await _context.Users.FindAsync(senderId);
   var receiver = await _context.Users.FindAsync(receiverId);
   ```

5. **Map sang DTO:**
   ```csharp
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
   ```

**Return:** `Task<MessageDto>`

---

#### **Hàm: `GetOrCreateConversationAsync(int user1Id, int user2Id)`**

**Tham số:**
- `user1Id: int` - ID user 1
- `user2Id: int` - ID user 2

**Logic xử lý:**

1. **Đảm bảo user1Id < user2Id:**
   ```csharp
   var (minId, maxId) = user1Id < user2Id ? (user1Id, user2Id) : (user2Id, user1Id);
   ```
   - Để tránh duplicate conversation (ví dụ: conversation giữa user 1 và 2 = conversation giữa user 2 và 1)

2. **Tìm conversation:**
   ```csharp
   var conversation = await _context.Conversations
       .FirstOrDefaultAsync(c => 
           (c.User1Id == minId && c.User2Id == maxId) ||
           (c.User1Id == maxId && c.User2Id == minId));
   ```
   - Query PostgreSQL: `SELECT * FROM Conversations WHERE (User1Id = @minId AND User2Id = @maxId) OR (User1Id = @maxId AND User2Id = @minId)`

3. **Nếu không tồn tại → Tạo mới:**
   ```csharp
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
   ```

4. **Trả về conversation:**
   ```csharp
   return conversation;
   ```

**Return:** `Task<Conversation>`

---

### Frontend: chat.component.ts

#### **Hàm: `loadConversations()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Gọi API:**
   ```typescript
   const response = await this.chatService.getConversations().toPromise();
   ```

2. **Xử lý response:**
   ```typescript
   if (response?.success) {
       this.conversations = response.data;
   }
   ```

---

### Frontend: chat.service.ts

#### **Hàm: `getConversations()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Gọi API:**
   ```typescript
   return this.http.get<any>(`${this.apiUrl}/Messages/conversations`, {
       headers: this.getHeaders()
   });
   ```
   - URL: `https://localhost:5001/api/Messages/conversations`
   - Headers: `Authorization: Bearer {token}`

**Return:** `Observable<any>`

---

### Backend: MessagesController.cs

#### **Hàm: `GetConversations()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Lấy current user:**
   ```csharp
   var userId = await GetCurrentUserIdAsync();
   if (userId == null)
   {
       return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
   }
   ```
   - Gọi `GetCurrentUserIdAsync()` → Parse token từ header → Validate token → Trả về `userId`

2. **Gọi MessageService:**
   ```csharp
   var conversations = await _messageService.GetConversationsAsync(userId.Value);
   ```

3. **Trả về:**
   ```csharp
   return Ok(new { success = true, data = conversations });
   ```

**Return:** `IActionResult`

---

#### **Hàm: `GetCurrentUserIdAsync()`**

**Tham số:** Không có

**Logic xử lý:**

1. **Lấy token từ header:**
   ```csharp
   var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
   if (string.IsNullOrEmpty(token))
   {
       token = Request.Query["token"].ToString();
   }
   ```

2. **Validate token:**
   ```csharp
   if (string.IsNullOrEmpty(token))
       return null;
   var user = await _authService.ValidateTokenAsync(token);
   return user?.Id;
   ```

**Return:** `Task<int?>` - User ID nếu hợp lệ, `null` nếu không

---

### Backend: MessageService.cs

#### **Hàm: `GetConversationsAsync(int userId)`**

**Tham số:**
- `userId: int` - ID của user hiện tại

**Logic xử lý:**

1. **Query conversations:**
   ```csharp
   var conversations = await _context.Conversations
       .Where(c => c.User1Id == userId || c.User2Id == userId)
       .Include(c => c.User1)
       .Include(c => c.User2)
       .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
       .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
       .ToListAsync();
   ```
   - Lấy tất cả conversations mà user tham gia
   - Include User1 và User2 để lấy thông tin
   - Include last message (1 message mới nhất)
   - Sắp xếp theo `LastMessageAt` (mới nhất trước)

2. **Map sang DTO:**
   ```csharp
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
           OtherIsOnline = otherUser.IsOnline,
           LastMessage = lastMessage != null ? new MessageDto { ... } : null,
           LastMessageAt = conv.LastMessageAt ?? conv.CreatedAt,
           UnreadCount = await _context.Messages
               .CountAsync(m => m.ConversationId == conv.Id && 
                               m.ReceiverId == userId && 
                               !m.IsRead)
       });
   }
   ```

**Return:** `Task<List<ConversationDto>>`

---

### Frontend: chat.component.ts

#### **Hàm: `selectConversation(conversation: ConversationDto)`**

**Tham số:**
- `conversation: ConversationDto` - Conversation được chọn

**Logic xử lý:**

1. **Set selected conversation:**
   ```typescript
   this.selectedConversation = conversation;
   this.isLoading = true;
   ```

2. **Load messages:**
   ```typescript
   const response = await this.chatService.getMessages(conversation.otherUserId).toPromise();
   if (response?.success) {
       this.messages = response.data;
       this.scrollToBottom();
   }
   ```

---

### Frontend: chat.service.ts

#### **Hàm: `getMessages(otherUserId: number, page: number = 1, pageSize: number = 50)`**

**Tham số:**
- `otherUserId: number` - ID người chat với
- `page: number` - Trang (mặc định 1)
- `pageSize: number` - Số message mỗi trang (mặc định 50)

**Logic xử lý:**

1. **Gọi API:**
   ```typescript
   return this.http.get<any>(`${this.apiUrl}/Messages/conversation/${otherUserId}`, {
       headers: this.getHeaders(),
       params: { page: page.toString(), pageSize: pageSize.toString() }
   });
   ```
   - URL: `https://localhost:5001/api/Messages/conversation/2?page=1&pageSize=50`

**Return:** `Observable<any>`

---

### Backend: MessagesController.cs

#### **Hàm: `GetMessages(int otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)`**

**Tham số:**
- `otherUserId: int` - ID người chat với
- `page: int` - Trang (mặc định 1)
- `pageSize: int` - Số message mỗi trang (mặc định 50)

**Logic xử lý:**

1. **Lấy current user:**
   ```csharp
   var userId = await GetCurrentUserIdAsync();
   ```

2. **Gọi MessageService:**
   ```csharp
   var messages = await _messageService.GetMessagesAsync(userId.Value, otherUserId, page, pageSize);
   ```

3. **Trả về:**
   ```csharp
   return Ok(new { success = true, data = messages });
   ```

**Return:** `IActionResult`

---

### Backend: MessageService.cs

#### **Hàm: `GetMessagesAsync(int userId, int otherUserId, int page = 1, int pageSize = 50)`**

**Tham số:**
- `userId: int` - ID user hiện tại
- `otherUserId: int` - ID người chat với
- `page: int` - Trang
- `pageSize: int` - Số message mỗi trang

**Logic xử lý:**

1. **Lấy conversation:**
   ```csharp
   var conversation = await GetOrCreateConversationAsync(userId, otherUserId);
   ```

2. **Query messages với pagination:**
   ```csharp
   var messages = await _context.Messages
       .Where(m => m.ConversationId == conversation.Id)
       .Include(m => m.Sender)
       .Include(m => m.Receiver)
       .OrderByDescending(m => m.SentAt)
       .Skip((page - 1) * pageSize)
       .Take(pageSize)
       .OrderBy(m => m.SentAt) // Đảo lại để hiển thị từ cũ đến mới
       .ToListAsync();
   ```
   - Query PostgreSQL với pagination
   - Sắp xếp mới nhất trước → Lấy `pageSize` messages → Đảo lại để hiển thị từ cũ đến mới

3. **Map sang DTO:**
   ```csharp
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
   ```

**Return:** `Task<List<MessageDto>>`

---

## 🍃 PHẦN 4: CHAT MONGODB

### Frontend: chat.component.ts

#### **Hàm: `selectMongoUser(user: UserDto)`**

**Tham số:**
- `user: UserDto` - User được chọn trong Mongo view

**Logic xử lý:**

1. **Set selected user:**
   ```typescript
   this.mongoSelectedUser = user;
   this.mongoLoading = true;
   ```

2. **Load lịch sử Mongo:**
   ```typescript
   const response = await this.chatService.getMongoConversation(user.id).toPromise();
   if (response?.success) {
       this.mongoMessages = response.data;
   }
   ```

---

### Frontend: chat.service.ts

#### **Hàm: `getMongoConversation(otherUserId: number, limit: number = 100)`**

**Tham số:**
- `otherUserId: number` - ID người chat với
- `limit: number` - Số message tối đa (mặc định 100)

**Logic xử lý:**

1. **Gọi API:**
   ```typescript
   return this.http.get<any>(`${this.apiUrl}/Messages/mongo/history/${otherUserId}`, {
       headers: this.getHeaders(),
       params: { limit: limit.toString() }
   });
   ```
   - URL: `https://localhost:5001/api/Messages/mongo/history/2?limit=100`

**Return:** `Observable<any>`

---

### Backend: MessagesController.cs

#### **Hàm: `GetMongoHistory(int otherUserId, [FromQuery] int limit = 100)`**

**Tham số:**
- `otherUserId: int` - ID người chat với
- `limit: int` - Số message tối đa (mặc định 100)

**Logic xử lý:**

1. **Lấy current user:**
   ```csharp
   var userId = await GetCurrentUserIdAsync();
   ```

2. **Gọi MongoChatService:**
   ```csharp
   var messages = await _mongoChatService.GetHistoryAsync(userId.Value, otherUserId, limit);
   ```

3. **Trả về:**
   ```csharp
   return Ok(new { success = true, data = messages });
   ```

**Return:** `IActionResult`

---

### Backend: MongoChatService.cs

#### **Hàm: `GetHistoryAsync(int userId, int otherUserId, int limit = 100)`**

**Tham số:**
- `userId: int` - ID user hiện tại
- `otherUserId: int` - ID người chat với
- `limit: int` - Số message tối đa

**Logic xử lý:**

1. **Tính conversationId:**
   ```csharp
   var convId = ComputeConversationId(userId, otherUserId);
   ```
   - Gọi `ComputeConversationId()` → `(min * 1_000_000) + max`
   - Ví dụ: user 1 và 2 → `convId = (1 * 1_000_000) + 2 = 1_000_002`

2. **Query MongoDB:**
   ```csharp
   var filter = Builders<MongoChatDocument>.Filter.Eq(x => x.ConversationId, convId);
   var docs = await _collection.Find(filter)
       .SortByDescending(x => x.SentAt)
       .Limit(limit)
       .ToListAsync();
   ```
   - Query MongoDB: `db.messages.find({ ConversationId: 1000002 }).sort({ SentAt: -1 }).limit(100)`

3. **Map sang DTO:**
   ```csharp
   return docs
       .OrderBy(x => x.SentAt) // Đảo lại để hiển thị từ cũ đến mới
       .Select(x => new MessageDto
       {
           Id = 0, // MongoDB dùng ObjectId, không có int ID
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
   ```

**Return:** `Task<List<MessageDto>>`

---

#### **Hàm: `ComputeConversationId(int user1, int user2)`**

**Tham số:**
- `user1: int` - ID user 1
- `user2: int` - ID user 2

**Logic xử lý:**

1. **Tìm min và max:**
   ```csharp
   var min = Math.Min(user1, user2);
   var max = Math.Max(user1, user2);
   ```

2. **Ghép thành số duy nhất:**
   ```csharp
   return (min * 1_000_000) + max;
   ```
   - Giả sử `userId < 1_000_000`
   - Ví dụ: user 1 và 2 → `(1 * 1_000_000) + 2 = 1_000_002`
   - Ví dụ: user 5 và 10 → `(5 * 1_000_000) + 10 = 5_000_010`

**Return:** `int` - ConversationId duy nhất

---

### Frontend: chat.component.ts

#### **Hàm: `sendMongoMessage()`**

**Tham số:** Không có (gọi từ button click)

**Logic xử lý:**

1. **Validate:**
   ```typescript
   if (!this.mongoNewMessage.trim() || !this.mongoSelectedUser) return;
   ```

2. **Lấy content và clear input:**
   ```typescript
   const content = this.mongoNewMessage.trim();
   this.mongoNewMessage = '';
   ```

3. **Gửi qua API Mongo:**
   ```typescript
   await this.chatService.sendMessageMongo(
       this.mongoSelectedUser.id,
       content
   ).toPromise();
   ```
   - Tin nhắn sẽ được đẩy về qua SignalR event `"MongoMessageSent"`

---

### Frontend: chat.service.ts

#### **Hàm: `sendMessageMongo(receiverId: number, content: string)`**

**Tham số:**
- `receiverId: number` - ID người nhận
- `content: string` - Nội dung tin nhắn

**Logic xử lý:**

1. **Gọi API:**
   ```typescript
   return this.http.post<any>(`${this.apiUrl}/Messages/mongo/send`, {
       receiverId,
       content
   }, {
       headers: this.getHeaders()
   });
   ```
   - URL: `https://localhost:5001/api/Messages/mongo/send`
   - Method: `POST`
   - Body: `{ receiverId, content }`

**Return:** `Observable<any>`

---

### Backend: MessagesController.cs

#### **Hàm: `SendMongoMessage([FromBody] SendMessageRequest request)`**

**Tham số:**
- `request: SendMessageRequest`
  - `ReceiverId: int`
  - `Content: string`

**Logic xử lý:**

1. **Lấy current user:**
   ```csharp
   var userId = await GetCurrentUserIdAsync();
   ```

2. **Validate:**
   ```csharp
   if (string.IsNullOrEmpty(request.Content))
   {
       return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống" });
   }
   ```

3. **Gọi MongoChatService:**
   ```csharp
   var message = await _mongoChatService.SendMessageAsync(userId.Value, request.ReceiverId, request.Content);
   ```
   - Lưu vào MongoDB

4. **Push realtime qua SignalR:**
   ```csharp
   await _hubContext.Clients.Group($"user_{request.ReceiverId}")
       .SendAsync("ReceiveMongoMessage", message);
   await _hubContext.Clients.Group($"user_{userId.Value}")
       .SendAsync("MongoMessageSent", message);
   ```
   - Gửi event `"ReceiveMongoMessage"` tới receiver
   - Gửi event `"MongoMessageSent"` tới sender

5. **Trả về:**
   ```csharp
   return Ok(new { success = true, data = message });
   ```

**Return:** `IActionResult`

---

### Backend: MongoChatService.cs

#### **Hàm: `SendMessageAsync(int senderId, int receiverId, string content)`**

**Tham số:**
- `senderId: int` - ID người gửi
- `receiverId: int` - ID người nhận
- `content: string` - Nội dung tin nhắn

**Logic xử lý:**

1. **Tính conversationId:**
   ```csharp
   var convId = ComputeConversationId(senderId, receiverId);
   ```

2. **Tạo document:**
   ```csharp
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
   ```

3. **Lưu vào MongoDB:**
   ```csharp
   await _collection.InsertOneAsync(doc);
   ```
   - INSERT vào collection `messages` trong MongoDB

4. **Map sang DTO:**
   ```csharp
   return new MessageDto
   {
       Id = 0, // MongoDB dùng ObjectId
       ConversationId = convId,
       SenderId = senderId,
       SenderUsername = senderId.ToString(),
       ReceiverId = receiverId,
       ReceiverUsername = receiverId.ToString(),
       Content = content,
       SentAt = now,
       IsRead = false
   };
   ```

**Return:** `Task<MessageDto>`

---

## 📊 Sơ đồ Luồng Chạy Tổng Quan

### Luồng Đăng Nhập:

```
1. User nhập username/password
   ↓
2. LoginComponent.onSubmit()
   ↓
3. AuthService.login()
   ↓
4. HTTP POST /api/Auth/login
   ↓
5. AuthController.Login()
   ↓
6. AuthService.LoginAsync()
   ├─→ Tìm user trong PostgreSQL
   ├─→ VerifyPassword()
   ├─→ Update IsOnline = true
   ├─→ GenerateSimpleToken()
   └─→ Return AuthResponse
   ↓
7. Frontend nhận response
   ↓
8. AuthService.setCurrentUser()
   ├─→ Lưu vào localStorage
   └─→ Emit qua BehaviorSubject
   ↓
9. window.location.reload()
   ↓
10. AppComponent detect user → Hiển thị ChatComponent
```

### Luồng Gửi Tin Nhắn SQL/SignalR:

```
1. User nhập tin nhắn và bấm Gửi
   ↓
2. ChatComponent.sendMessage()
   ↓
3. ChatService.sendMessage()
   ↓
4. SignalR invoke('SendMessage', receiverId, content)
   ↓
5. ChatHub.SendMessage()
   ├─→ Lấy senderId từ connection mapping
   ├─→ MessageService.SendMessageAsync()
   │   ├─→ GetOrCreateConversationAsync()
   │   ├─→ Tạo Message entity
   │   ├─→ SaveChangesAsync() → Lưu vào PostgreSQL
   │   └─→ Return MessageDto
   ├─→ Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", messageDto)
   └─→ Clients.Caller.SendAsync("MessageSent", messageDto)
   ↓
6. Frontend nhận event "ReceiveMessage" hoặc "MessageSent"
   ↓
7. ChatService.messageReceived$ hoặc messageSent$
   ↓
8. ChatComponent subscribe → Thêm message vào list → Hiển thị
```

### Luồng Gửi Tin Nhắn MongoDB:

```
1. User nhập tin nhắn trong Mongo view và bấm "Gửi Mongo"
   ↓
2. ChatComponent.sendMongoMessage()
   ↓
3. ChatService.sendMessageMongo()
   ↓
4. HTTP POST /api/Messages/mongo/send
   ↓
5. MessagesController.SendMongoMessage()
   ├─→ GetCurrentUserIdAsync()
   ├─→ MongoChatService.SendMessageAsync()
   │   ├─→ ComputeConversationId()
   │   ├─→ Tạo MongoChatDocument
   │   ├─→ InsertOneAsync() → Lưu vào MongoDB
   │   └─→ Return MessageDto
   ├─→ _hubContext.Clients.Group($"user_{receiverId}").SendAsync("ReceiveMongoMessage", message)
   └─→ _hubContext.Clients.Group($"user_{senderId}").SendAsync("MongoMessageSent", message)
   ↓
6. Frontend nhận event "ReceiveMongoMessage" hoặc "MongoMessageSent"
   ↓
7. ChatService.mongoMessageReceived$ hoặc mongoMessageSent$
   ↓
8. ChatComponent subscribe → Thêm message vào mongoMessages → Hiển thị
```

---

## 🔄 Luồng Real-time (SignalR Events)

### Khi User Kết Nối:

```
1. Frontend: ChatService.startConnection()
   ↓
2. SignalR WebSocket connection tới /chatHub?token=...
   ↓
3. Backend: ChatHub.OnConnectedAsync()
   ├─→ Parse token từ query string
   ├─→ AuthService.ValidateTokenAsync()
   ├─→ Lưu connection mapping: _userConnections[userId] = connectionId
   ├─→ Groups.AddToGroupAsync(connectionId, $"user_{userId}")
   └─→ Clients.All.SendAsync("UserOnline", userId)
   ↓
4. Frontend: ChatService.userOnline$ → Update UI (user.isOnline = true)
```

### Khi User Ngắt Kết Nối:

```
1. SignalR WebSocket disconnect
   ↓
2. Backend: ChatHub.OnDisconnectedAsync()
   ├─→ Xóa connection mapping: _userConnections.Remove(userId)
   └─→ Clients.All.SendAsync("UserOffline", userId)
   ↓
3. Frontend: ChatService.userOffline$ → Update UI (user.isOnline = false)
```

---

## 📝 Tóm Tắt Các Hàm Chính

### Backend:

| Hàm | File | Tham số | Logic | Return |
|-----|------|---------|-------|--------|
| `LoginAsync` | AuthService.cs | `LoginRequest` | Tìm user → Verify password → Update online → Generate token | `AuthResponse?` |
| `ValidateTokenAsync` | AuthService.cs | `string token` | Parse token → Tìm user trong DB | `User?` |
| `SendMessageAsync` | MessageService.cs | `senderId, receiverId, content` | Get/create conversation → Lưu vào PostgreSQL | `MessageDto` |
| `GetMessagesAsync` | MessageService.cs | `userId, otherUserId, page, pageSize` | Query PostgreSQL với pagination | `List<MessageDto>` |
| `GetConversationsAsync` | MessageService.cs | `userId` | Query conversations + last message + unread count | `List<ConversationDto>` |
| `SendMessageAsync` | MongoChatService.cs | `senderId, receiverId, content` | Compute conversationId → Lưu vào MongoDB | `MessageDto` |
| `GetHistoryAsync` | MongoChatService.cs | `userId, otherUserId, limit` | Query MongoDB với filter conversationId | `List<MessageDto>` |
| `SendMessage` | ChatHub.cs | `receiverId, content` | Lấy senderId → Lưu SQL → Push SignalR | `Task` |
| `OnConnectedAsync` | ChatHub.cs | - | Validate token → Lưu connection → Add group | `Task` |

### Frontend:

| Hàm | File | Tham số | Logic | Return |
|-----|------|---------|-------|--------|
| `login` | auth.service.ts | `LoginRequest` | POST /api/Auth/login → Lưu user | `Observable` |
| `startConnection` | chat.service.ts | - | Tạo SignalR connection → Register handlers | `Promise<void>` |
| `sendMessage` | chat.service.ts | `receiverId, content` | SignalR invoke('SendMessage') | `Promise<void>` |
| `sendMessageMongo` | chat.service.ts | `receiverId, content` | POST /api/Messages/mongo/send | `Observable` |
| `getConversations` | chat.service.ts | - | GET /api/Messages/conversations | `Observable` |
| `getMessages` | chat.service.ts | `otherUserId, page, pageSize` | GET /api/Messages/conversation/{id} | `Observable` |
| `sendMessage` | chat.component.ts | - | Gọi ChatService.sendMessage() → Nhận qua SignalR | `Promise<void>` |
| `sendMongoMessage` | chat.component.ts | - | Gọi ChatService.sendMessageMongo() → Nhận qua SignalR | `Promise<void>` |
| `setupSignalRHandlers` | chat.component.ts | - | Subscribe các event streams → Update UI | `void` |

---

**Tài liệu này mô tả chi tiết từng hàm, tham số, logic xử lý và luồng chạy của toàn bộ hệ thống. Bạn có thể tham khảo để hiểu rõ cách hệ thống hoạt động! 🚀**

