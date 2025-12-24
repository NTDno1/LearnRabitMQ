# Kiến Trúc Microservice - Tài Liệu Chi Tiết

## 📐 Tổng Quan Kiến Trúc

Dự án này triển khai kiến trúc Microservice dựa trên các nguyên tắc từ giáo trình "Các Hệ Thống Phân Tán" và best practices thực tế.

## 🏛️ Các Thành Phần Chính

### 1. API Gateway (Ocelot)

**Vai trò**: Điểm vào duy nhất cho tất cả các requests từ client

**Chức năng**:
- Điều hướng requests đến các microservices phù hợp
- Load balancing
- Rate limiting (có thể mở rộng)
- Authentication/Authorization (có thể mở rộng)

**Port**: 5000

### 2. User Service

**Domain**: Quản lý người dùng

**Chức năng**:
- CRUD operations cho users
- Authentication (có thể mở rộng)
- User profile management

**Database**: userservice_db (PostgreSQL)

**Port**: 5001

**API Endpoints**:
- `GET /api/users` - Lấy danh sách users
- `GET /api/users/{id}` - Lấy user theo ID
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

### 3. Product Service

**Domain**: Quản lý sản phẩm

**Chức năng**:
- CRUD operations cho products
- Quản lý inventory (stock)
- Category management

**Database**: productservice_db (PostgreSQL)

**Port**: 5002

**API Endpoints**:
- `GET /api/products` - Lấy danh sách products
- `GET /api/products/{id}` - Lấy product theo ID
- `GET /api/products/category/{category}` - Lấy products theo category
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `DELETE /api/products/{id}` - Xóa product
- `PATCH /api/products/{id}/stock` - Cập nhật stock

### 4. Order Service

**Domain**: Quản lý đơn hàng

**Chức năng**:
- CRUD operations cho orders
- Order status management
- Tích hợp với Product Service và User Service
- Publish events qua RabbitMQ

**Database**: orderservice_db (PostgreSQL)

**Port**: 5003

**Message Queue**: RabbitMQ
- Queue: `order.created`
- Queue: `order.status.updated`

**API Endpoints**:
- `GET /api/orders` - Lấy danh sách orders
- `GET /api/orders/{id}` - Lấy order theo ID
- `GET /api/orders/user/{userId}` - Lấy orders của user
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}/status` - Cập nhật status
- `DELETE /api/orders/{id}` - Xóa order

## 🔄 Luồng Giao Tiếp

### Synchronous Communication (HTTP/REST)

```
Client → API Gateway → User Service → PostgreSQL
Client → API Gateway → Product Service → PostgreSQL
Client → API Gateway → Order Service → PostgreSQL
```

**Lưu ý:** Tất cả requests từ client đều đi qua API Gateway.

### Asynchronous Communication (RabbitMQ)

```
Order Service → RabbitMQ (trực tiếp, không qua Gateway)
                ↓
        [Other Services can subscribe]
```

**Lưu ý:** RabbitMQ được các services sử dụng **trực tiếp**, không qua API Gateway.

### Infrastructure Services

```
Tất cả Services → MongoDB (trực tiếp, không qua Gateway)
                  - Logging
                  - Events storage

Order Service → RabbitMQ (trực tiếp, không qua Gateway)
                  - Event publishing
                  - Message queue
```

**Lưu ý:** MongoDB và RabbitMQ là **infrastructure services** được các microservices sử dụng trực tiếp.

**Events được publish**:
1. `OrderCreated`: Khi order mới được tạo
   - Data: OrderId, UserId, TotalAmount, OrderItems
   
2. `OrderStatusUpdated`: Khi status của order thay đổi
   - Data: OrderId, OldStatus, NewStatus

## 🗄️ Database Design

### Database Per Service Pattern

Mỗi microservice có database riêng để đảm bảo:
- **Độc lập**: Có thể deploy và scale độc lập
- **Bảo mật**: Dữ liệu được phân tán và bảo vệ
- **Linh hoạt**: Có thể chọn công nghệ database phù hợp

### Schema Overview

#### userservice_db (PostgreSQL)
- **Users**: Thông tin người dùng

#### productservice_db (PostgreSQL)
- **Products**: Thông tin sản phẩm

#### orderservice_db (PostgreSQL)
- **Orders**: Thông tin đơn hàng
- **OrderItems**: Chi tiết items trong order

## 📦 Shared Libraries

### Microservice.Common

Chứa các thành phần dùng chung:
- **BaseEntity**: Base class cho tất cả entities
- **MessageEvent**: Model cho events
- **IMessagePublisher**: Interface cho message publishing
- **IMessageConsumer**: Interface cho message consuming

## 🔐 Security Considerations

### Hiện tại:
- CORS được cấu hình để cho phép tất cả origins (chỉ cho development)
- Password được hash bằng BCrypt

### Có thể mở rộng:
- JWT Authentication
- API Key management
- Rate limiting
- Request validation
- Input sanitization

## 📈 Scalability

### Horizontal Scaling

Mỗi service có thể được scale độc lập:
```bash
docker-compose up -d --scale user-service=3
docker-compose up -d --scale product-service=2
```

### Load Balancing

API Gateway có thể được cấu hình để load balance giữa các instances của cùng một service.

## 🔍 Monitoring & Logging

### Logging
- Mỗi service sử dụng ILogger để log
- Logs có thể được xem qua `docker-compose logs`

### Health Checks
- Health endpoint: `/api/health` (có thể thêm vào mỗi service)
- Docker health checks được cấu hình trong docker-compose.yml

## 🚀 Deployment

### Development
- Docker Compose để chạy tất cả services cùng lúc
- Hot reload khi code thay đổi (nếu mount volumes)

### Production (Có thể mở rộng)
- Kubernetes cho orchestration
- Service mesh (Istio/Linkerd) cho service-to-service communication
- Centralized logging (ELK stack)
- Distributed tracing (Jaeger/Zipkin)
- Monitoring (Prometheus + Grafana)

## 📚 Nguyên Tắc Thiết Kế

Dựa trên giáo trình "Các Hệ Thống Phân Tán":

1. **Tính độc lập (Independence)**: Mỗi service độc lập về deployment và database
2. **Gắn kết lỏng (Loose Coupling)**: Services giao tiếp qua API và message queue
3. **Tính mô đun (Modularity)**: Mỗi service tập trung vào một domain cụ thể
4. **Tính trong suốt (Transparency)**: API Gateway che giấu sự phức tạp
5. **Khả năng mở rộng (Scalability)**: Dễ dàng scale từng service
6. **Tính chịu lỗi (Fault Tolerance)**: Một service lỗi không ảnh hưởng toàn bộ hệ thống

## 🔄 Event-Driven Architecture

OrderService sử dụng event-driven pattern:
- Khi order được tạo → Publish `OrderCreated` event
- Các services khác có thể subscribe để xử lý:
  - ProductService: Cập nhật stock
  - NotificationService: Gửi email/SMS
  - PaymentService: Xử lý thanh toán

## 📝 Best Practices Đã Áp Dụng

1. ✅ Database per service
2. ✅ API Gateway pattern
3. ✅ Event-driven communication
4. ✅ Containerization với Docker
5. ✅ Configuration externalization
6. ✅ Logging và error handling
7. ✅ CORS configuration
8. ✅ Swagger documentation

## 🔮 Có Thể Mở Rộng

1. **Service Discovery**: Consul hoặc Eureka
2. **Configuration Server**: Spring Cloud Config hoặc Consul KV
3. **Circuit Breaker**: Polly hoặc Resilience4j
4. **Distributed Tracing**: OpenTelemetry
5. **API Versioning**: URL versioning hoặc header versioning
6. **Caching**: Redis cho distributed caching
7. **Message Broker**: Thêm Kafka cho high-throughput scenarios

