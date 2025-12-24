# Kiến Trúc Microservice - Backend Project

Dự án này triển khai một hệ thống backend theo mô hình kiến trúc Microservice sử dụng .NET 8.0, dựa trên các nguyên tắc từ giáo trình "Các Hệ Thống Phân Tán" và các best practices thực tế.

## 📖 Hướng Dẫn Chạy Dự Án

**👉 Xem file [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md) để có hướng dẫn chi tiết từng bước!**

## 🎬 Demo và Kịch Bản

**👉 Xem file [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md) để có kịch bản demo chi tiết!**

**👉 Xem file [TONG_QUAN_DU_AN.md](./TONG_QUAN_DU_AN.md) để hiểu tổng quan về dự án!**

## 🎨 Frontend

**👉 Xem thư mục [Frontend](./Frontend/) để có Angular app demo!**

## 📋 Mô Tả Dự Án

Hệ thống bao gồm các microservices độc lập, mỗi service quản lý một domain cụ thể:

- **UserService**: Quản lý người dùng (CRUD operations)
- **ProductService**: Quản lý sản phẩm (CRUD operations)
- **OrderService**: Quản lý đơn hàng với tích hợp RabbitMQ cho giao tiếp bất đồng bộ
- **ApiGateway**: Điều hướng requests đến các microservices sử dụng Ocelot

## 🏗️ Kiến Trúc Hệ Thống

```
┌─────────────┐
│   Client    │
│  (Frontend) │
└──────┬──────┘
       │ HTTP
       ▼
┌─────────────────┐
│   API Gateway    │ (Port 5000)
│    (Ocelot)     │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┐
    │        │          │          │
    ▼        ▼          ▼          │
┌────────┐ ┌────────┐ ┌────────┐  │
│ User   │ │Product │ │ Order  │  │
│Service │ │Service │ │Service │  │
│ :5001  │ │ :5002  │ │ :5003  │  │
└────┬───┘ └────┬───┘ └────┬───┘  │
     │         │          │       │
     ▼         ▼          ▼       │
┌──────────────┐ ┌──────────────┐ │ ┌──────────────┐
│userservice_db│ │productservice│ │ │orderservice_db│
│ (PostgreSQL) │ │   _db       │ │ │ (PostgreSQL) │
│              │ │ (PostgreSQL)│ │ │              │
└──────────────┘ └──────────────┘ │ └──────────────┘
     │         │          │       │
     └─────────┴──────────┴───────┘
                    │
        ┌───────────┴───────────┐
        │                       │
        ▼                       ▼
┌──────────────┐        ┌──────────────┐
│   MongoDB    │        │   RabbitMQ   │
│ (Logging/    │        │ (Message     │
│  Events)     │        │  Queue)     │
│              │        │              │
│ Tất cả       │        │ Order Service│
│ Services     │        │ sử dụng      │
│ sử dụng      │        │              │
└──────────────┘        └──────────────┘
```

**Lưu ý:** MongoDB và RabbitMQ là **infrastructure services** được các microservices sử dụng trực tiếp, không qua API Gateway.

## 🛠️ Công Nghệ Sử Dụng

- **.NET 8.0**: Framework chính
- **Entity Framework Core**: ORM cho database operations
- **PostgreSQL**: Database chính cho mỗi microservice (Npgsql)
- **MongoDB**: NoSQL database cho logging và events
- **RabbitMQ**: Message queue cho giao tiếp bất đồng bộ
- **Ocelot**: API Gateway
- **Docker & Docker Compose**: Containerization
- **BCrypt.Net**: Password hashing

## 📁 Cấu Trúc Dự Án

```
Microservice/
├── Microservice.Common/              # Shared libraries
│   ├── Models/
│   └── Interfaces/
├── Microservice.ApiGateway/         # API Gateway
│   ├── Controllers/
│   └── ocelot.json
├── Microservice.Services.UserService/    # User Microservice
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Data/
│   └── DTOs/
├── Microservice.Services.ProductService/ # Product Microservice
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Data/
│   └── DTOs/
├── Microservice.Services.OrderService/   # Order Microservice
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Data/
│   └── DTOs/
└── docker-compose.yml
```

## 🚀 Cài Đặt và Chạy

### Yêu Cầu Hệ Thống

- .NET 8.0 SDK
- Docker Desktop (nếu chạy bằng Docker)
- **PostgreSQL**: Đang sử dụng từ server `47.130.33.106:5432`
- **MongoDB**: Đang sử dụng từ MongoDB Atlas
- **RabbitMQ**: Đang sử dụng từ server `47.130.33.106:5672`

**Lưu ý**: Các databases đang được cấu hình để sử dụng từ server external. Nếu muốn chạy local, cần cập nhật connection strings trong `appsettings.json`.

### Chạy Bằng Docker Compose (Khuyến nghị)

1. Clone repository và di chuyển vào thư mục Microservice:
```bash
cd Microservice
```

2. Chạy toàn bộ hệ thống:
```bash
docker-compose up -d
```

3. Kiểm tra các services đã chạy:
```bash
docker-compose ps
```

4. Truy cập các endpoints:
   - **API Gateway Swagger**: http://localhost:5000/swagger
   - **User Service Swagger**: http://localhost:5001/swagger
   - **Product Service Swagger**: http://localhost:5002/swagger
   - **Order Service Swagger**: http://localhost:5003/swagger
   - **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### Chạy Local (Development)

**Lưu ý**: Dự án đang sử dụng PostgreSQL và MongoDB từ server external. Connection strings đã được cấu hình sẵn trong `appsettings.json`.

1. **Kiểm tra kết nối đến databases**:
   - PostgreSQL: `47.130.33.106:5432`
   - MongoDB: MongoDB Atlas (connection string trong appsettings.json)
   - RabbitMQ: `47.130.33.106:5672`

2. **Nếu muốn chạy databases local**, cập nhật connection strings trong `appsettings.json` của mỗi service:
   - PostgreSQL local: `Host=localhost;Port=5432;Database=...;Username=postgres;Password=...`
   - MongoDB local: `mongodb://localhost:27017`

3. Chạy từng service:
```bash
# Terminal 1 - User Service
cd Microservice.Services.UserService
dotnet run

# Terminal 2 - Product Service
cd Microservice.Services.ProductService
dotnet run

# Terminal 3 - Order Service
cd Microservice.Services.OrderService
dotnet run

# Terminal 4 - API Gateway
cd Microservice.ApiGateway
dotnet run
```

## 📡 API Endpoints

### User Service (qua API Gateway)

- `GET /api/users` - Lấy danh sách tất cả users
- `GET /api/users/{id}` - Lấy thông tin user theo ID
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

### Product Service (qua API Gateway)

- `GET /api/products` - Lấy danh sách tất cả products
- `GET /api/products/{id}` - Lấy thông tin product theo ID
- `GET /api/products/category/{category}` - Lấy products theo category
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `DELETE /api/products/{id}` - Xóa product
- `PATCH /api/products/{id}/stock` - Cập nhật stock

### Order Service (qua API Gateway)

- `GET /api/orders` - Lấy danh sách tất cả orders
- `GET /api/orders/{id}` - Lấy thông tin order theo ID
- `GET /api/orders/user/{userId}` - Lấy orders của user
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}/status` - Cập nhật status của order
- `DELETE /api/orders/{id}` - Xóa order

## 🔄 Giao Tiếp Bất Đồng Bộ

OrderService sử dụng RabbitMQ để publish các events:

- **order.created**: Khi một order mới được tạo
- **order.status.updated**: Khi status của order thay đổi

Các services khác có thể subscribe vào các queues này để xử lý events.

## 📝 Nguyên Tắc Thiết Kế

Dự án tuân theo các nguyên tắc từ giáo trình "Các Hệ Thống Phân Tán":

1. **Tính độc lập**: Mỗi microservice có database riêng và có thể triển khai độc lập
2. **Gắn kết lỏng**: Các services giao tiếp qua API và message queue
3. **Tính mô đun**: Mỗi service tập trung vào một domain cụ thể
4. **Tính trong suốt**: API Gateway che giấu sự phức tạp của hệ thống phân tán
5. **Khả năng mở rộng**: Dễ dàng scale từng service độc lập

## 🧪 Testing với Swagger UI

✅ **Tất cả các services đều có Swagger UI được cấu hình và luôn được bật** (không chỉ trong Development mode):

- **API Gateway Swagger**: http://localhost:5000/swagger
  - Title: API Gateway
  - Mô tả: Điểm vào duy nhất cho tất cả các API requests
  
- **User Service Swagger**: http://localhost:5001/swagger
  - Title: User Service API
  - Mô tả: API cho quản lý người dùng
  
- **Product Service Swagger**: http://localhost:5002/swagger
  - Title: Product Service API
  - Mô tả: API cho quản lý sản phẩm
  
- **Order Service Swagger**: http://localhost:5003/swagger
  - Title: Order Service API
  - Mô tả: API cho quản lý đơn hàng với tích hợp RabbitMQ

**Đặc điểm**:
- Swagger UI luôn được bật ở mọi môi trường (Development, Staging, Production)
- Mỗi service có thông tin mô tả riêng trong Swagger
- Dễ dàng test và tương tác với APIs trực tiếp từ trình duyệt

## 📚 Tài Liệu Tham Khảo

- Giáo trình "Các Hệ Thống Phân Tán" - Học viện Công nghệ Bưu chính Viễn thông
- [Microservices Patterns](https://microservices.io/patterns/)
- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)

## 🔧 Troubleshooting

### Lỗi kết nối PostgreSQL
- Kiểm tra PostgreSQL server `47.130.33.106:5432` có thể truy cập được không
- Kiểm tra connection string trong appsettings.json
- Đảm bảo database đã được tạo: `userservice_db`, `productservice_db`, `orderservice_db`
- Kiểm tra username/password: `postgres/123456`

### Lỗi kết nối MongoDB
- Kiểm tra MongoDB connection string trong appsettings.json
- Đảm bảo MongoDB Atlas cluster đang hoạt động
- Kiểm tra network access trong MongoDB Atlas (whitelist IP nếu cần)

### Lỗi kết nối RabbitMQ
- Kiểm tra RabbitMQ server `47.130.33.106:5672` có thể truy cập được không
- Kiểm tra credentials trong appsettings.json: `guest/guest`
- Kiểm tra firewall/network rules

### Lỗi API Gateway không route được
- Kiểm tra file ocelot.json
- Đảm bảo các services đã chạy trước khi start API Gateway
- Kiểm tra ports trong ocelot.json khớp với ports của services

## 📄 License

Dự án này được tạo cho mục đích học tập và nghiên cứu.

## 👥 Tác Giả

Dự án được phát triển dựa trên giáo trình "Các Hệ Thống Phân Tán" và các best practices thực tế về microservices architecture.

