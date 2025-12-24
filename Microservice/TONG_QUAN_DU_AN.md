# 📊 Tổng Quan Dự Án Microservice

## 🎯 Mục Đích Dự Án

Dự án này triển khai một hệ thống **E-Commerce Backend** theo mô hình kiến trúc **Microservice**, minh họa các nguyên tắc và best practices của hệ thống phân tán.

> **💡 Lưu ý:** MongoDB và RabbitMQ là **infrastructure services** được các microservices sử dụng trực tiếp, không qua API Gateway. Xem [GIAI_THICH_KIEN_TRUC.md](./GIAI_THICH_KIEN_TRUC.md) để hiểu rõ hơn.

## 🏗️ Kiến Trúc Tổng Thể

```
┌─────────────────────────────────────────────────────────┐
│                    FRONTEND (Angular)                    │
│              http://localhost:4200                        │
└──────────────────────┬───────────────────────────────────┘
                       │ HTTP Requests
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  API GATEWAY (Ocelot)                    │
│              http://localhost:5000                       │
│  - Điều hướng requests                                   │
│  - Load balancing                                       │
│  - Single entry point                                    │
└──────┬──────────────┬──────────────┬─────────────────────┘
       │              │              │
       │              │              │
       ▼              ▼              ▼
┌──────────┐   ┌──────────┐   ┌──────────┐
│  USER    │   │ PRODUCT  │   │  ORDER   │
│ SERVICE  │   │ SERVICE  │   │ SERVICE  │
│  :5001   │   │  :5002   │   │  :5003   │
└────┬─────┘   └────┬─────┘   └────┬─────┘
     │              │              │
     │              │              │
     ▼              ▼              ▼
┌──────────┐   ┌──────────┐   ┌──────────┐
│PostgreSQL│   │PostgreSQL│   │PostgreSQL│
│userservice│   │productservice│ │orderservice│
│   _db    │   │   _db    │   │   _db    │
└──────────┘   └──────────┘   └──────────┘
     │              │              │
     │              │              │
     └──────────────┴──────────────┴──────────────┐
                                                  │
                    ┌─────────────────────────────┴─────────────┐
                    │                                           │
                    ▼                                           ▼
            ┌─────────────────┐                      ┌─────────────────┐
            │    MongoDB       │                      │    RabbitMQ      │
            │  (Logging/Events)│                      │  (Message Queue) │
            │                  │                      │                  │
            │ - microservice_  │                      │ - order.created  │
            │   users          │                      │ - order.status.  │
            │ - microservice_  │                      │   updated        │
            │   products       │                      │                  │
            │ - microservice_  │                      │ 47.130.33.106    │
            │   orders         │                      │ :5672            │
            └─────────────────┘                      └─────────────────┘
                    ▲                                           ▲
                    │                                           │
                    └───────────────────────────────────────────┘
                              (Các Services sử dụng)
```

**Giải thích:**
- **MongoDB** và **RabbitMQ** là các **infrastructure services** (dịch vụ hạ tầng)
- Chúng được các **microservices sử dụng trực tiếp**, không qua API Gateway
- **MongoDB**: Dùng cho logging và events (tất cả services)
- **RabbitMQ**: Dùng cho message queue (chủ yếu Order Service)
- Chúng hoạt động **song song** với các microservices, không phải đứng trước hay sau API Gateway

## 🎨 Các Tính Năng Chính

### 1. 👥 User Service - Quản Lý Người Dùng

**Chức năng:**
- ✅ Đăng ký tài khoản mới
- ✅ Xem danh sách người dùng
- ✅ Xem chi tiết người dùng
- ✅ Cập nhật thông tin người dùng
- ✅ Xóa người dùng (soft delete)
- ✅ Quản lý trạng thái hoạt động

**API Endpoints:**
- `GET /api/users` - Lấy danh sách users
- `GET /api/users/{id}` - Lấy user theo ID
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

**Database:** PostgreSQL (`userservice_db`)

### 2. 📦 Product Service - Quản Lý Sản Phẩm

**Chức năng:**
- ✅ Xem danh sách sản phẩm
- ✅ Tìm kiếm sản phẩm theo category
- ✅ Thêm sản phẩm mới
- ✅ Cập nhật thông tin sản phẩm
- ✅ Quản lý tồn kho (stock)
- ✅ Xóa sản phẩm

**API Endpoints:**
- `GET /api/products` - Lấy danh sách products
- `GET /api/products/{id}` - Lấy product theo ID
- `GET /api/products/category/{category}` - Lấy products theo category
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `PATCH /api/products/{id}/stock` - Cập nhật stock
- `DELETE /api/products/{id}` - Xóa product

**Database:** PostgreSQL (`productservice_db`)

### 3. 🛒 Order Service - Quản Lý Đơn Hàng

**Chức năng:**
- ✅ Tạo đơn hàng mới
- ✅ Xem danh sách đơn hàng
- ✅ Xem đơn hàng theo user
- ✅ Cập nhật trạng thái đơn hàng
- ✅ Xóa đơn hàng
- ✅ Tích hợp RabbitMQ để publish events

**API Endpoints:**
- `GET /api/orders` - Lấy danh sách orders
- `GET /api/orders/{id}` - Lấy order theo ID
- `GET /api/orders/user/{userId}` - Lấy orders của user
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}/status` - Cập nhật status
- `DELETE /api/orders/{id}` - Xóa order

**Database:** PostgreSQL (`orderservice_db`)

**Message Queue Events:**
- `order.created` - Khi đơn hàng mới được tạo
- `order.status.updated` - Khi trạng thái đơn hàng thay đổi

### 4. 🚪 API Gateway - Điều Hướng Requests

**Chức năng:**
- ✅ Single entry point cho tất cả requests
- ✅ Route requests đến đúng microservice
- ✅ Load balancing
- ✅ CORS configuration
- ✅ Swagger documentation

**Port:** 5000

## 🛠️ Công Nghệ Sử Dụng

### Backend:
- **.NET 8.0** - Framework chính
- **Entity Framework Core** - ORM
- **PostgreSQL** - Relational database
- **MongoDB** - NoSQL database cho logging
- **RabbitMQ** - Message queue
- **Ocelot** - API Gateway
- **Swagger/OpenAPI** - API documentation

### Frontend (sẽ tạo):
- **Angular 17+** - Framework
- **Angular Material** - UI components
- **RxJS** - Reactive programming
- **HttpClient** - API communication

## 📈 Luồng Hoạt Động

### 1. Luồng Tạo Đơn Hàng:

```
User (Frontend)
    ↓
API Gateway (Port 5000)
    ↓
Order Service (Port 5003)
    ├─→ Lưu vào PostgreSQL
    ├─→ Publish event "order.created" → RabbitMQ
    └─→ Response về Frontend
```

### 2. Luồng Xem Sản Phẩm:

```
User (Frontend)
    ↓ HTTP Request
API Gateway (Port 5000)
    ↓ Route request
Product Service (Port 5002)
    ├─→ Query từ PostgreSQL (productservice_db)
    ├─→ Log → MongoDB (trực tiếp, không qua Gateway)
    └─→ Response về Frontend (qua API Gateway)
```

### 3. Luồng Quản Lý User:

```
User (Frontend)
    ↓ HTTP Request
API Gateway (Port 5000)
    ↓ Route request
User Service (Port 5001)
    ├─→ CRUD operations với PostgreSQL (userservice_db)
    ├─→ Log → MongoDB (trực tiếp, không qua Gateway)
    └─→ Response về Frontend (qua API Gateway)
```

## 🎯 Điểm Nổi Bật

### 1. Kiến Trúc Microservice
- ✅ Mỗi service độc lập, có database riêng
- ✅ Có thể deploy và scale độc lập
- ✅ Fault isolation - một service lỗi không ảnh hưởng toàn bộ hệ thống

### 2. Giao Tiếp Bất Đồng Bộ
- ✅ Sử dụng RabbitMQ cho event-driven communication
- ✅ Order Service publish events khi có thay đổi
- ✅ Các services khác có thể subscribe để xử lý

### 3. API Gateway Pattern
- ✅ Single entry point
- ✅ Che giấu sự phức tạp của hệ thống phân tán
- ✅ Dễ dàng thêm authentication, rate limiting

### 4. Database Per Service
- ✅ Mỗi service có database riêng
- ✅ Đảm bảo tính độc lập
- ✅ Có thể chọn công nghệ database phù hợp

### 5. Swagger Documentation
- ✅ Tất cả services có Swagger UI
- ✅ Dễ dàng test và tương tác với APIs
- ✅ Luôn được bật ở mọi môi trường

## 📊 Dữ Liệu Mẫu

### Users:
- Admin users
- Customer users
- Test users

### Products:
- Electronics (Laptop, Phone, Tablet)
- Clothing (Shirt, Pants, Shoes)
- Books (Technical, Fiction)

### Orders:
- Orders với nhiều order items
- Các trạng thái: Pending, Processing, Shipped, Delivered

## 🔐 Bảo Mật (Có Thể Mở Rộng)

- ✅ Password hashing với BCrypt
- ✅ CORS configuration
- ⏳ JWT Authentication (có thể thêm)
- ⏳ Role-based Authorization (có thể thêm)
- ⏳ Rate Limiting (có thể thêm)

## 📈 Khả Năng Mở Rộng

### Horizontal Scaling:
- Mỗi service có thể scale độc lập
- Load balancing qua API Gateway
- Stateless services

### Monitoring (Có Thể Thêm):
- ⏳ Health checks
- ⏳ Distributed tracing
- ⏳ Logging aggregation
- ⏳ Metrics collection

## 🎓 Mục Đích Học Tập

Dự án này minh họa:
1. ✅ Kiến trúc Microservice
2. ✅ Service-to-service communication
3. ✅ Event-driven architecture
4. ✅ API Gateway pattern
5. ✅ Database per service pattern
6. ✅ Containerization với Docker
7. ✅ Best practices trong .NET

## 📝 Tài Liệu Liên Quan

- [README.md](./README.md) - Tổng quan dự án
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Kiến trúc chi tiết
- [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md) - Hướng dẫn chạy
- [QUICKSTART.md](./QUICKSTART.md) - Hướng dẫn nhanh

