# 📖 Hướng Dẫn Chạy Dự Án Microservice

## 📋 Mục Lục
1. [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
2. [Chuẩn Bị Databases](#chuẩn-bị-databases)
3. [Cách 1: Chạy Local (Khuyến Nghị)](#cách-1-chạy-local-khuyến-nghị)
4. [Cách 2: Chạy Bằng Docker](#cách-2-chạy-bằng-docker)
5. [Kiểm Tra và Test](#kiểm-tra-và-test)
6. [Troubleshooting](#troubleshooting)

---

## ✅ Yêu Cầu Hệ Thống

### Phần Mềm Cần Cài Đặt:

1. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Kiểm tra: `dotnet --version` (phải >= 8.0.0)

2. **Visual Studio 2022** hoặc **Visual Studio Code** (tùy chọn)
   - VS Code: https://code.visualstudio.com/
   - Extension: C# Dev Kit

3. **PostgreSQL Client** (để kiểm tra database - tùy chọn)
   - pgAdmin hoặc DBeaver

4. **Docker Desktop** (nếu chạy bằng Docker)
   - Download: https://www.docker.com/products/docker-desktop

### Kết Nối Mạng:

- ✅ Kết nối được đến PostgreSQL server: `47.130.33.106:5432`
- ✅ Kết nối được đến RabbitMQ server: `47.130.33.106:5672`
- ✅ Kết nối được đến MongoDB Atlas (internet)

---

## 🗄️ Chuẩn Bị Databases

### Bước 1: Tạo Databases trong PostgreSQL

Kết nối đến PostgreSQL server `47.130.33.106:5432` và tạo 3 databases:

```sql
-- Kết nối PostgreSQL (dùng pgAdmin, DBeaver, hoặc psql)
-- Server: 47.130.33.106
-- Port: 5432
-- Username: postgres
-- Password: 123456

-- Tạo database cho User Service
CREATE DATABASE userservice_db;

-- Tạo database cho Product Service
CREATE DATABASE productservice_db;

-- Tạo database cho Order Service
CREATE DATABASE orderservice_db;
```

**Hoặc dùng lệnh psql:**
```bash
psql -h 47.130.33.106 -p 5432 -U postgres -d postgres

# Sau đó chạy các lệnh CREATE DATABASE ở trên
```

### Bước 2: Kiểm Tra MongoDB

MongoDB đã được cấu hình sẵn trong `appsettings.json`. Đảm bảo:
- ✅ MongoDB Atlas cluster đang hoạt động
- ✅ Connection string đúng trong appsettings.json
- ✅ Network access đã được cấu hình (whitelist IP nếu cần)

### Bước 3: Kiểm Tra RabbitMQ

RabbitMQ đã được cấu hình sẵn:
- ✅ Server: `47.130.33.106:5672`
- ✅ Username: `guest`
- ✅ Password: `guest`

---

## 🚀 Cách 1: Chạy Local (Khuyến Nghị)

### ⚡ Cách Nhanh: Sử Dụng Script PowerShell (Khuyến Nghị)

**Windows PowerShell:**
```powershell
# Chạy tất cả services tự động
.\run-all-services.ps1

# Dừng tất cả services
.\stop-all-services.ps1
```

Script sẽ tự động:
- ✅ Kiểm tra .NET SDK
- ✅ Restore packages
- ✅ Build solution
- ✅ Mở 4 cửa sổ PowerShell riêng cho mỗi service

### 📝 Cách Thủ Công: Chạy Từng Service

### Bước 1: Mở Terminal/PowerShell

Mở **4 terminal windows** riêng biệt (mỗi service chạy trong 1 terminal).

### Bước 2: Restore Packages

Trong terminal đầu tiên, chạy lệnh restore packages cho toàn bộ solution:

```bash
cd Microservice
dotnet restore
```

### Bước 3: Chạy User Service

**Terminal 1:**
```bash
cd Microservice/Microservice.Services.UserService
dotnet run
```

**Kết quả mong đợi:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
      Now listening on: https://localhost:5002
```

**Swagger UI:** http://localhost:5001/swagger

### Bước 4: Chạy Product Service

**Terminal 2:**
```bash
cd Microservice/Microservice.Services.ProductService
dotnet run
```

**Kết quả mong đợi:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5002
      Now listening on: https://localhost:5003
```

**Swagger UI:** http://localhost:5002/swagger

### Bước 5: Chạy Order Service

**Terminal 3:**
```bash
cd Microservice/Microservice.Services.OrderService
dotnet run
```

**Kết quả mong đợi:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5003
      Now listening on: https://localhost:5004
```

**Swagger UI:** http://localhost:5003/swagger

### Bước 6: Chạy API Gateway

**Terminal 4:**
```bash
cd Microservice/Microservice.ApiGateway
dotnet run
```

**Kết quả mong đợi:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

**Swagger UI:** http://localhost:5000/swagger

### ⚠️ Lưu Ý:

- **Thứ tự chạy**: Nên chạy các services trước (User, Product, Order), sau đó mới chạy API Gateway
- **Ports**: Đảm bảo các ports 5000, 5001, 5002, 5003 không bị chiếm bởi ứng dụng khác
- **Database**: Lần đầu chạy, Entity Framework sẽ tự động tạo tables (EnsureCreated)

---

## 🐳 Cách 2: Chạy Bằng Docker

### Bước 1: Kiểm Tra Docker

```bash
docker --version
docker-compose --version
```

### Bước 2: Build và Chạy

```bash
cd Microservice
docker-compose up -d --build
```

**Lệnh này sẽ:**
- Build images cho tất cả services
- Tạo containers
- Chạy tất cả services trong background

### Bước 3: Kiểm Tra Trạng Thái

```bash
docker-compose ps
```

**Kết quả mong đợi:**
```
NAME                        STATUS              PORTS
microservice-api-gateway    Up                  0.0.0.0:5000->8080/tcp
microservice-user-service   Up                  0.0.0.0:5001->8080/tcp
microservice-product-service Up                  0.0.0.0:5002->8080/tcp
microservice-order-service  Up                  0.0.0.0:5003->8080/tcp
```

### Bước 4: Xem Logs

```bash
# Xem logs của tất cả services
docker-compose logs -f

# Xem logs của một service cụ thể
docker-compose logs -f user-service
docker-compose logs -f product-service
docker-compose logs -f order-service
docker-compose logs -f api-gateway
```

### Bước 5: Dừng Services

```bash
# Dừng tất cả services
docker-compose down

# Dừng và xóa volumes (nếu có)
docker-compose down -v
```

---

## 🧪 Kiểm Tra và Test

### 1. Kiểm Tra Swagger UI

Mở trình duyệt và truy cập:

- ✅ **API Gateway**: http://localhost:5000/swagger
- ✅ **User Service**: http://localhost:5001/swagger
- ✅ **Product Service**: http://localhost:5002/swagger
- ✅ **Order Service**: http://localhost:5003/swagger

### 2. Test API Qua Swagger

#### Tạo User Mới:

1. Mở http://localhost:5000/swagger
2. Tìm endpoint `POST /api/users`
3. Click "Try it out"
4. Nhập JSON:
```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "password123",
  "firstName": "Test",
  "lastName": "User"
}
```
5. Click "Execute"
6. Kiểm tra response (status 201 Created)

#### Tạo Product Mới:

1. Tìm endpoint `POST /api/products`
2. Nhập JSON:
```json
{
  "name": "Laptop Dell",
  "description": "High performance laptop",
  "price": 15000000,
  "stock": 10,
  "category": "Electronics"
}
```
3. Execute và kiểm tra response

#### Tạo Order:

1. Tìm endpoint `POST /api/orders`
2. Nhập JSON:
```json
{
  "userId": 1,
  "shippingAddress": "123 Main St, Hanoi",
  "orderItems": [
    {
      "productId": 1,
      "quantity": 2
    }
  ]
}
```
3. Execute và kiểm tra response

### 3. Test Bằng cURL (PowerShell/CMD)

#### Tạo User:
```powershell
curl -X POST http://localhost:5000/api/users `
  -H "Content-Type: application/json" `
  -d '{\"username\":\"testuser\",\"email\":\"test@example.com\",\"password\":\"password123\",\"firstName\":\"Test\",\"lastName\":\"User\"}'
```

#### Lấy Danh Sách Users:
```powershell
curl http://localhost:5000/api/users
```

#### Tạo Product:
```powershell
curl -X POST http://localhost:5000/api/products `
  -H "Content-Type: application/json" `
  -d '{\"name\":\"Laptop\",\"description\":\"High performance laptop\",\"price\":15000000,\"stock\":10,\"category\":\"Electronics\"}'
```

### 4. Kiểm Tra Database

Kết nối PostgreSQL và kiểm tra:

```sql
-- Kiểm tra User Service database
\c userservice_db
\dt  -- Xem danh sách tables
SELECT * FROM "Users";

-- Kiểm tra Product Service database
\c productservice_db
\dt
SELECT * FROM "Products";

-- Kiểm tra Order Service database
\c orderservice_db
\dt
SELECT * FROM "Orders";
```

---

## 🔧 Troubleshooting

### ❌ Lỗi: "Connection refused" khi kết nối PostgreSQL

**Nguyên nhân:**
- PostgreSQL server không truy cập được
- Firewall chặn port 5432
- Connection string sai

**Giải pháp:**
1. Kiểm tra kết nối mạng đến `47.130.33.106:5432`
2. Kiểm tra connection string trong `appsettings.json`
3. Thử kết nối bằng pgAdmin hoặc psql

### ❌ Lỗi: "Port already in use"

**Nguyên nhân:**
- Port đã bị chiếm bởi ứng dụng khác

**Giải pháp:**
```bash
# Windows: Tìm process đang dùng port
netstat -ano | findstr :5001

# Kill process (thay PID bằng process ID)
taskkill /PID <PID> /F

# Hoặc đổi port trong appsettings.json hoặc launchSettings.json
```

### ❌ Lỗi: "Database does not exist"

**Nguyên nhân:**
- Database chưa được tạo

**Giải pháp:**
1. Tạo database như hướng dẫn ở phần [Chuẩn Bị Databases](#chuẩn-bị-databases)
2. Hoặc Entity Framework sẽ tự tạo nếu có quyền (EnsureCreated)

### ❌ Lỗi: "RabbitMQ connection failed"

**Nguyên nhân:**
- RabbitMQ server không truy cập được
- Credentials sai

**Giải pháp:**
1. Kiểm tra kết nối đến `47.130.33.106:5672`
2. Kiểm tra credentials trong `appsettings.json`: `guest/guest`
3. Kiểm tra firewall rules

### ❌ Lỗi: "MongoDB connection failed"

**Nguyên nhân:**
- MongoDB Atlas cluster không truy cập được
- Connection string sai
- Network access chưa được cấu hình

**Giải pháp:**
1. Kiểm tra connection string trong `appsettings.json`
2. Kiểm tra MongoDB Atlas dashboard
3. Whitelist IP address trong MongoDB Atlas Network Access

### ❌ Lỗi: "Cannot find ocelot.json"

**Nguyên nhân:**
- File ocelot.json không có trong thư mục API Gateway

**Giải pháp:**
1. Kiểm tra file `Microservice.ApiGateway/ocelot.json` có tồn tại
2. Đảm bảo file được copy khi build

### ❌ Service không start được

**Giải pháp:**
```bash
# Xem logs chi tiết
dotnet run --verbosity detailed

# Hoặc với Docker
docker-compose logs -f [service-name]

# Kiểm tra dependencies
dotnet restore
dotnet build
```

### ❌ Lỗi: "Package not found"

**Giải pháp:**
```bash
# Restore packages
dotnet restore

# Clean và rebuild
dotnet clean
dotnet restore
dotnet build
```

---

## 📝 Checklist Trước Khi Chạy

- [ ] Đã cài đặt .NET 8.0 SDK
- [ ] Đã tạo 3 databases trong PostgreSQL (userservice_db, productservice_db, orderservice_db)
- [ ] Đã kiểm tra kết nối đến PostgreSQL server (47.130.33.106:5432)
- [ ] Đã kiểm tra kết nối đến RabbitMQ server (47.130.33.106:5672)
- [ ] Đã kiểm tra MongoDB connection string
- [ ] Đã restore packages: `dotnet restore`
- [ ] Đã kiểm tra ports 5000, 5001, 5002, 5003 không bị chiếm

---

## 🎯 Kết Quả Mong Đợi

Sau khi chạy thành công, bạn sẽ có:

✅ **4 services đang chạy:**
- API Gateway: http://localhost:5000
- User Service: http://localhost:5001
- Product Service: http://localhost:5002
- Order Service: http://localhost:5003

✅ **Swagger UI** cho tất cả services

✅ **Databases** đã được tạo tables tự động

✅ **Có thể test APIs** qua Swagger hoặc cURL

---

## 💡 Tips

1. **Sử dụng Visual Studio Code** với extension C# để debug dễ dàng hơn
2. **Sử dụng Postman** để test APIs thay vì Swagger nếu muốn
3. **Xem logs** thường xuyên để phát hiện lỗi sớm
4. **Backup databases** trước khi test các tính năng mới
5. **Sử dụng Git** để quản lý code và rollback nếu cần

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề, hãy:
1. Kiểm tra phần [Troubleshooting](#troubleshooting)
2. Xem logs chi tiết
3. Kiểm tra connection strings trong `appsettings.json`
4. Đảm bảo tất cả yêu cầu đã được đáp ứng

**Chúc bạn thành công! 🚀**

