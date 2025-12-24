# Hướng Dẫn Nhanh - Microservice Architecture

## 🚀 Khởi Động Nhanh

### Bước 1: Kiểm tra yêu cầu

Đảm bảo bạn đã cài đặt:
- Docker Desktop
- .NET 8.0 SDK (nếu chạy local)

### Bước 2: Chạy bằng Docker Compose

```bash
cd Microservice
docker-compose up -d
```

Chờ vài phút để tất cả services khởi động.

### Bước 3: Kiểm tra services

```bash
# Kiểm tra trạng thái
docker-compose ps

# Xem logs
docker-compose logs -f
```

### Bước 4: Test API với Swagger UI

Mở trình duyệt và truy cập Swagger UI của các services:
- **API Gateway Swagger**: http://localhost:5000/swagger
- **User Service Swagger**: http://localhost:5001/swagger
- **Product Service Swagger**: http://localhost:5002/swagger
- **Order Service Swagger**: http://localhost:5003/swagger
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

**Lưu ý**: 
- ✅ Tất cả Swagger UI đều **luôn được bật** (không chỉ trong Development mode)
- ✅ Mỗi service có thông tin mô tả riêng trong Swagger
- ✅ Có thể test APIs trực tiếp từ Swagger UI

### Bước 5: Test với cURL hoặc Postman

#### Tạo User mới:
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "password123",
    "firstName": "Test",
    "lastName": "User"
  }'
```

#### Tạo Product mới:
```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop",
    "description": "High performance laptop",
    "price": 15000000,
    "stock": 10,
    "category": "Electronics"
  }'
```

#### Tạo Order:
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "shippingAddress": "123 Main St, Hanoi",
    "orderItems": [
      {
        "productId": 1,
        "quantity": 2
      }
    ]
  }'
```

## 🛑 Dừng Services

```bash
docker-compose down
```

Để xóa cả volumes (database data):
```bash
docker-compose down -v
```

## 🔍 Debugging

### Xem logs của một service cụ thể:
```bash
docker-compose logs -f user-service
docker-compose logs -f product-service
docker-compose logs -f order-service
docker-compose logs -f api-gateway
```

### Vào trong container:
```bash
docker exec -it microservice-user-service bash
```

### Kiểm tra database:
```bash
docker exec -it microservice-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P YourStrong@Passw0rd \
  -Q "SELECT name FROM sys.databases"
```

## 📝 Lưu Ý

1. **Ports đã sử dụng**:
   - 5000: API Gateway
   - 5001: User Service
   - 5002: Product Service
   - 5003: Order Service
   - 5432: PostgreSQL (external server)
   - 5672: RabbitMQ (external server)
   - 15672: RabbitMQ Management (nếu có)

2. **Database**: Mỗi service có database riêng trong PostgreSQL:
   - userservice_db
   - productservice_db
   - orderservice_db

3. **RabbitMQ**: 
   - Server: 47.130.33.106:5672
   - Username/Password: `guest/guest`

## 🐛 Troubleshooting

### Service không start được
```bash
# Xem logs chi tiết
docker-compose logs [service-name]

# Restart service
docker-compose restart [service-name]
```

### Database connection error
- Kiểm tra PostgreSQL server `47.130.33.106:5432` có thể truy cập được không
- Kiểm tra connection strings trong appsettings.json
- Đảm bảo 3 databases đã được tạo: userservice_db, productservice_db, orderservice_db

### RabbitMQ connection error
- Kiểm tra RabbitMQ container đã chạy: `docker ps | grep rabbitmq`
- Kiểm tra ports 5672 và 15672

