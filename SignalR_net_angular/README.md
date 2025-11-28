# SignalR Real-time Notification System

Dự án mô phỏng hệ thống thông báo/cập nhật trạng thái thời gian thực sử dụng **ASP.NET Core SignalR** và **RabbitMQ** với Frontend **Angular**.

## 📋 Kiến trúc hệ thống

```
RabbitMQ Queue → RabbitMQ Consumer Service → SignalR Hub → Angular Clients
```

1. **RabbitMQ**: Lưu trữ tin nhắn trong hàng đợi
2. **RabbitMQ Consumer Service**: Lắng nghe tin nhắn từ RabbitMQ
3. **SignalR Hub**: Phát tin nhắn tới tất cả client đã kết nối
4. **Angular Client**: Hiển thị thông báo real-time

## 🛠️ Yêu cầu hệ thống

- **.NET 8.0 SDK** hoặc mới hơn
- **Node.js 18+** và **npm**
- **Angular CLI 17+**
- **RabbitMQ Server** (đã có sẵn tại `47.130.33.106`)
- **PostgreSQL** (đã có sẵn tại `47.130.33.106:5432`)

## 📁 Cấu trúc dự án

```
SignalR_net_angular/
├── Backend/                    # .NET Core Backend
│   ├── Hubs/
│   │   └── NotificationHub.cs # SignalR Hub
│   ├── Services/
│   │   └── RabbitMQConsumerService.cs # RabbitMQ Consumer
│   ├── Controllers/
│   │   └── TestController.cs   # API test gửi tin nhắn
│   ├── Program.cs              # Cấu hình ứng dụng
│   └── appsettings.json        # Cấu hình
│
└── Frontend/                   # Angular Frontend
    └── src/
        └── app/
            ├── app.component.ts    # Component chính
            └── services/
                └── signalr.service.ts # SignalR Client Service
```

---

## 🚀 Hướng dẫn cài đặt và chạy

### Bước 1: Thiết lập Backend (.NET Core)

#### 1.1. Cài đặt dependencies

Mở terminal trong thư mục `Backend` và chạy:

```bash
cd Backend
dotnet restore
```

#### 1.2. Kiểm tra cấu hình

Mở file `Backend/appsettings.json` và kiểm tra cấu hình RabbitMQ:

```json
{
  "RabbitMQ": {
    "HostName": "47.130.33.106",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

#### 1.3. Chạy Backend

```bash
dotnet run
```

Backend sẽ chạy tại:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **SignalR Hub**: `http://localhost:5000/notificationHub`
- **Swagger UI**: `http://localhost:5000/swagger`

#### 1.4. Kiểm tra Backend đã chạy

- Mở trình duyệt và truy cập: `http://localhost:5000/swagger`
- Bạn sẽ thấy API `POST /api/Test/send-message` để test gửi tin nhắn

---

### Bước 2: Thiết lập Frontend (Angular)

#### 2.1. Cài đặt Angular CLI (nếu chưa có)

```bash
npm install -g @angular/cli@17
```

#### 2.2. Cài đặt dependencies

Mở terminal trong thư mục `Frontend` và chạy:

```bash
cd Frontend
npm install
```

Lệnh này sẽ cài đặt:
- Angular framework
- `@microsoft/signalr` - Thư viện SignalR client cho Angular
- Các dependencies khác

#### 2.3. Kiểm tra cấu hình SignalR URL

Mở file `Frontend/src/app/services/signalr.service.ts` và kiểm tra URL:

```typescript
const hubUrl = 'http://localhost:5000/notificationHub';
```

Đảm bảo URL này khớp với URL của Backend SignalR Hub.

#### 2.4. Chạy Frontend

```bash
ng serve
```

Hoặc:

```bash
npm start
```

Frontend sẽ chạy tại: `http://localhost:4200`

#### 2.5. Mở ứng dụng

Mở trình duyệt và truy cập: `http://localhost:4200`

---

## 📖 Giải thích chi tiết về Frontend (Angular)

### 2.1. Cấu trúc Component

#### **app.component.ts** - Component chính

Đây là component chính của ứng dụng, có các chức năng:

```typescript
// 1. Quản lý danh sách thông báo
notifications: Notification[] = [];

// 2. Theo dõi trạng thái kết nối
connectionStatus: 'connected' | 'disconnected' | 'connecting'

// 3. Kết nối tới SignalR khi component khởi tạo
ngOnInit(): void {
  this.connect();
}

// 4. Ngắt kết nối khi component bị hủy
ngOnDestroy(): void {
  this.disconnect();
}
```

**Các method quan trọng:**

- `connect()`: Gọi service để kết nối tới SignalR Hub
- `disconnect()`: Ngắt kết nối
- `addNotification()`: Thêm thông báo mới vào danh sách
- `clearNotifications()`: Xóa tất cả thông báo

#### **signalr.service.ts** - Service quản lý SignalR

Service này đóng vai trò trung gian giữa Angular và SignalR Hub:

```typescript
// 1. Tạo Hub Connection
this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect() // Tự động kết nối lại
  .build();

// 2. Đăng ký lắng nghe event từ Hub
this.hubConnection.on('ReceiveNotification', (message: string) => {
  // Phát thông báo tới component
  this.notificationSubject.next(message);
});

// 3. Bắt đầu kết nối
this.hubConnection.start()
```

**Các Observable (RxJS):**

- `notificationReceived$`: Phát thông báo mới tới component
- `connectionStatus$`: Phát trạng thái kết nối (connected/disconnected/connecting)

**Tại sao dùng Observable?**

- Observable cho phép nhiều component có thể subscribe và nhận thông báo
- Dễ dàng quản lý lifecycle (tự động unsubscribe khi component bị hủy)
- Hỗ trợ các operator của RxJS để xử lý dữ liệu

### 2.2. Luồng hoạt động

```
1. Angular App khởi động
   ↓
2. AppComponent.ngOnInit() được gọi
   ↓
3. Gọi SignalRService.startConnection()
   ↓
4. SignalR kết nối tới Backend Hub
   ↓
5. Đăng ký lắng nghe event "ReceiveNotification"
   ↓
6. Khi Backend gửi thông báo → SignalR nhận được
   ↓
7. SignalRService phát thông báo qua Observable
   ↓
8. AppComponent nhận thông báo và hiển thị
```

### 2.3. Template HTML (app.component.html)

Template sử dụng **Angular Directives**:

- `*ngIf`: Hiển thị/ẩn element dựa trên điều kiện
- `*ngFor`: Lặp qua mảng để hiển thị danh sách
- `(click)`: Xử lý sự kiện click
- `[disabled]`: Vô hiệu hóa button dựa trên điều kiện
- `{{ }}`: Interpolation - hiển thị giá trị biến

**Ví dụ:**

```html
<!-- Hiển thị nếu chưa có thông báo -->
<div *ngIf="notifications.length === 0">
  Chưa có thông báo
</div>

<!-- Lặp qua danh sách thông báo -->
<div *ngFor="let notification of notifications">
  {{ notification.message }}
</div>

<!-- Button với event binding -->
<button (click)="connect()">Kết nối</button>
```

### 2.4. Styling (CSS)

File `styles.css` chứa các style global, bao gồm:

- **Animation**: Hiệu ứng slideIn khi thông báo mới xuất hiện
- **Responsive**: Tự động điều chỉnh theo kích thước màn hình
- **Modern UI**: Sử dụng gradient, shadow, border-radius

---

## 🧪 Cách test hệ thống

### Cách 1: Sử dụng Swagger UI

1. Mở `http://localhost:5000/swagger`
2. Tìm API `POST /api/Test/send-message`
3. Click "Try it out"
4. Nhập tin nhắn (ví dụ: `"Xin chào từ RabbitMQ!"`)
5. Click "Execute"
6. Kiểm tra Frontend - thông báo sẽ xuất hiện ngay lập tức

### Cách 2: Sử dụng cURL

```bash
curl -X POST "http://localhost:5000/api/Test/send-message" \
  -H "Content-Type: application/json" \
  -d "\"Tin nhắn test từ cURL\""
```

### Cách 3: Sử dụng Postman

1. Tạo request mới: `POST http://localhost:5000/api/Test/send-message`
2. Headers: `Content-Type: application/json`
3. Body (raw JSON): `"Tin nhắn từ Postman"`
4. Send
5. Kiểm tra Frontend

---

## 🔍 Debugging

### Backend

- Kiểm tra logs trong console khi chạy `dotnet run`
- Logs sẽ hiển thị:
  - Khi client kết nối/ngắt kết nối
  - Khi nhận tin nhắn từ RabbitMQ
  - Lỗi nếu có

### Frontend

- Mở **Developer Tools** (F12) trong trình duyệt
- Tab **Console** sẽ hiển thị:
  - `SignalR Connection Started` - Kết nối thành công
  - `Received notification from Hub: ...` - Nhận thông báo
  - Các lỗi nếu có

### Kiểm tra kết nối SignalR

1. Mở Developer Tools (F12)
2. Tab **Network**
3. Tìm request tới `/notificationHub`
4. Kiểm tra status code (200 = thành công)

---

## ❓ Câu hỏi thường gặp (FAQ)

### Q1: Frontend không nhận được thông báo?

**Kiểm tra:**

1. Backend đã chạy chưa? (`http://localhost:5000/swagger`)
2. Frontend đã kết nối chưa? (Kiểm tra badge trạng thái)
3. CORS đã được cấu hình đúng chưa? (Kiểm tra `Program.cs`)
4. URL SignalR Hub đúng chưa? (Kiểm tra `signalr.service.ts`)

### Q2: Lỗi CORS?

**Giải pháp:**

Đảm bảo trong `Backend/Program.cs` có:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Quan trọng!
    });
});
```

### Q3: RabbitMQ không kết nối được?

**Kiểm tra:**

1. RabbitMQ server đang chạy tại `47.130.33.106:5672`
2. Username/Password đúng: `guest/guest`
3. Firewall không chặn port 5672

### Q4: Làm sao thay đổi port của Backend?

1. Sửa `Backend/Properties/launchSettings.json`
2. Sửa URL trong `Frontend/src/app/services/signalr.service.ts`

---

## 📚 Tài liệu tham khảo

- [ASP.NET Core SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Angular SignalR Client](https://www.npmjs.com/package/@microsoft/signalr)
- [RabbitMQ .NET Client](https://www.rabbitmq.com/dotnet.html)
- [RxJS Documentation](https://rxjs.dev/)

---

## 🎯 Mở rộng dự án

### Ý tưởng cải tiến:

1. **Lưu trữ thông báo vào Database**: Sử dụng PostgreSQL để lưu lịch sử
2. **Phân quyền người dùng**: Chỉ gửi thông báo cho user cụ thể
3. **Nhóm thông báo**: Tạo các nhóm (groups) trong SignalR
4. **UI/UX cải thiện**: Thêm sound notification, toast messages
5. **Error Handling**: Xử lý lỗi tốt hơn với retry mechanism

---

## 📝 License

MIT License

---

**Chúc bạn học tập vui vẻ! 🚀**

