# 🎬 Kịch Bản Demo Dự Án Microservice

## 📋 Chuẩn Bị Trước Khi Demo

### 1. Kiểm Tra Hệ Thống

✅ **Backend Services đang chạy:**
- API Gateway: http://localhost:5000
- User Service: http://localhost:5001
- Product Service: http://localhost:5002
- Order Service: http://localhost:5003

✅ **Frontend đang chạy:**
- Angular App: http://localhost:4200

✅ **Databases:**
- PostgreSQL: 3 databases đã được tạo
- MongoDB: Connection đã được cấu hình
- RabbitMQ: Server đang hoạt động

### 2. Mở Các Tab Trình Duyệt

1. **Tab 1:** Frontend Angular - http://localhost:4200
2. **Tab 2:** API Gateway Swagger - http://localhost:5000/swagger
3. **Tab 3:** User Service Swagger - http://localhost:5001/swagger
4. **Tab 4:** Product Service Swagger - http://localhost:5002/swagger
5. **Tab 5:** Order Service Swagger - http://localhost:5003/swagger
6. **Tab 6:** RabbitMQ Management (nếu có) - http://47.130.33.106:15672

---

## 🎯 PHẦN 1: GIỚI THIỆU TỔNG QUAN (2 phút)

### Mục Tiêu:
Giới thiệu kiến trúc Microservice và các thành phần chính

### Nội Dung:

1. **Mở Frontend** (http://localhost:4200)
   - Giới thiệu giao diện chính
   - Giải thích 3 modules: Users, Products, Orders

2. **Giải thích Kiến Trúc:**
   ```
   Frontend (Angular)
        ↓
   API Gateway (Ocelot) - Single Entry Point
        ↓
   ┌─────┬─────┬─────┐
   │User │Product│Order│
   │Service│Service│Service│
   └─────┴─────┴─────┘
   ```

3. **Điểm Nổi Bật:**
   - ✅ Mỗi service độc lập, có database riêng
   - ✅ API Gateway điều hướng requests
   - ✅ RabbitMQ cho giao tiếp bất đồng bộ
   - ✅ Swagger UI cho tất cả services

---

## 👥 PHẦN 2: DEMO USER SERVICE (5 phút)

### Mục Tiêu:
Minh họa CRUD operations với User Service

### Bước 1: Xem Danh Sách Users

**Trong Frontend:**
1. Click vào menu "Users" ở sidebar
2. Giải thích: Frontend gọi API Gateway → User Service → PostgreSQL
3. Hiển thị danh sách users (nếu có)

**Trong Swagger:**
1. Mở User Service Swagger (http://localhost:5001/swagger)
2. Test endpoint `GET /api/users`
3. Giải thích response structure

### Bước 2: Tạo User Mới

**Cách 1: Qua Swagger (Khuyến nghị cho demo)**

1. Mở API Gateway Swagger (http://localhost:5000/swagger)
2. Tìm endpoint `POST /api/users`
3. Click "Try it out"
4. Nhập JSON:
```json
{
  "username": "demo_user",
  "email": "demo@example.com",
  "password": "password123",
  "firstName": "Demo",
  "lastName": "User",
  "phoneNumber": "0123456789"
}
```
5. Click "Execute"
6. Giải thích:
   - Request đi qua API Gateway
   - API Gateway route đến User Service
   - User Service lưu vào PostgreSQL
   - Response trả về qua API Gateway

**Cách 2: Qua Frontend**
1. Click nút "Thêm User Mới" (hiện tại sẽ hướng dẫn dùng Swagger)
2. Refresh danh sách để thấy user mới

### Bước 3: Cập Nhật User

1. Trong Swagger, test `PUT /api/users/{id}`
2. Cập nhật firstName hoặc lastName
3. Giải thích: Soft update, UpdatedAt được cập nhật

### Bước 4: Xóa User

1. Trong Frontend, click nút Delete
2. Giải thích: Soft delete (IsDeleted = true)

---

## 📦 PHẦN 3: DEMO PRODUCT SERVICE (5 phút)

### Mục Tiêu:
Minh họa quản lý sản phẩm và tìm kiếm theo category

### Bước 1: Xem Danh Sách Products

**Trong Frontend:**
1. Click menu "Products"
2. Giải thích: Product Service có database riêng

**Trong Swagger:**
1. Test `GET /api/products`
2. Giải thích cấu trúc Product entity

### Bước 2: Tạo Products Mẫu

**Tạo 3-4 products qua Swagger:**

**Product 1 - Laptop:**
```json
{
  "name": "Laptop Dell XPS 15",
  "description": "High performance laptop for professionals",
  "price": 25000000,
  "stock": 10,
  "category": "Electronics"
}
```

**Product 2 - iPhone:**
```json
{
  "name": "iPhone 15 Pro",
  "description": "Latest iPhone with A17 chip",
  "price": 30000000,
  "stock": 5,
  "category": "Electronics"
}
```

**Product 3 - T-Shirt:**
```json
{
  "name": "Cotton T-Shirt",
  "description": "Comfortable cotton t-shirt",
  "price": 200000,
  "stock": 50,
  "category": "Clothing"
}
```

**Product 4 - Book:**
```json
{
  "name": "Clean Code",
  "description": "A Handbook of Agile Software Craftsmanship",
  "price": 300000,
  "stock": 20,
  "category": "Books"
}
```

### Bước 3: Lọc Theo Category

**Trong Frontend:**
1. Chọn category "Electronics" từ dropdown
2. Giải thích: Frontend gọi `GET /api/products/category/Electronics`
3. Chỉ hiển thị products thuộc category đó

**Trong Swagger:**
1. Test endpoint `GET /api/products/category/{category}`
2. Thử với các categories: Electronics, Clothing, Books

### Bước 4: Cập Nhật Stock

1. Trong Swagger, test `PATCH /api/products/{id}/stock`
2. Giải thích: Inventory management

---

## 🛒 PHẦN 4: DEMO ORDER SERVICE (8 phút) - QUAN TRỌNG NHẤT

### Mục Tiêu:
Minh họa tạo đơn hàng và event-driven architecture với RabbitMQ

### Bước 1: Xem Danh Sách Orders

**Trong Frontend:**
1. Click menu "Orders"
2. Giải thích: Order Service tích hợp với User và Product Services

### Bước 2: Tạo Đơn Hàng Mới

**Qua Swagger (API Gateway):**

1. Mở API Gateway Swagger
2. Tìm endpoint `POST /api/orders`
3. Click "Try it out"
4. Nhập JSON (đảm bảo userId và productId đã tồn tại):
```json
{
  "userId": 1,
  "shippingAddress": "123 Đường ABC, Quận 1, TP.HCM",
  "orderItems": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}
```
5. Click "Execute"

### Bước 3: Giải Thích Luồng Xử Lý

**Khi tạo đơn hàng:**

```
1. Frontend/Client → API Gateway
2. API Gateway → Order Service
3. Order Service:
   ├─ Lưu Order vào PostgreSQL (orderservice_db)
   ├─ Publish event "order.created" → RabbitMQ
   └─ Response về API Gateway → Client
```

**Giải thích:**
- ✅ Order được lưu vào database riêng
- ✅ Event được publish vào RabbitMQ
- ✅ Các services khác có thể subscribe để xử lý:
   - ProductService: Cập nhật stock
   - NotificationService: Gửi email (nếu có)
   - PaymentService: Xử lý thanh toán (nếu có)

### Bước 4: Xem Chi Tiết Đơn Hàng

**Trong Frontend:**
1. Click vào đơn hàng để xem chi tiết
2. Giải thích:
   - Order Items với thông tin sản phẩm
   - Total amount được tính tự động
   - Status: Pending (mặc định)

### Bước 5: Cập Nhật Trạng Thái Đơn Hàng

**Trong Frontend:**
1. Chọn status mới từ dropdown (ví dụ: "Processing")
2. Giải thích:
   - Frontend gọi `PUT /api/orders/{id}/status`
   - Order Service cập nhật status
   - Publish event "order.status.updated" → RabbitMQ

**Các trạng thái:**
- Pending → Processing → Shipped → Delivered
- Hoặc Cancelled

### Bước 6: Xem RabbitMQ Events (Nếu có Management UI)

1. Mở RabbitMQ Management (nếu có)
2. Giải thích các queues:
   - `order.created`
   - `order.status.updated`
3. Xem messages trong queues

---

## 🔄 PHẦN 5: DEMO KIẾN TRÚC PHÂN TÁN (3 phút)

### Mục Tiêu:
Minh họa tính độc lập và fault tolerance

### Bước 1: Giải Thích Database Per Service

1. Mở PostgreSQL và show 3 databases:
   - `userservice_db`
   - `productservice_db`
   - `orderservice_db`
2. Giải thích: Mỗi service có database riêng → Độc lập

### Bước 2: Giải Thích API Gateway

1. Mở API Gateway Swagger
2. Giải thích:
   - Client chỉ cần biết 1 endpoint: API Gateway
   - API Gateway route đến đúng service
   - Load balancing (nếu có nhiều instances)

### Bước 3: Fault Tolerance

**Giả sử:**
- Nếu Product Service down → User Service và Order Service vẫn hoạt động
- Nếu một service lỗi → Không ảnh hưởng services khác

---

## 📊 PHẦN 6: TỔNG KẾT VÀ Q&A (2 phút)

### Điểm Nổi Bật Đã Demo:

1. ✅ **Microservice Architecture**
   - Mỗi service độc lập
   - Database per service
   - Có thể deploy riêng

2. ✅ **API Gateway Pattern**
   - Single entry point
   - Route requests
   - Load balancing

3. ✅ **Event-Driven Architecture**
   - RabbitMQ cho async communication
   - Loose coupling giữa services

4. ✅ **RESTful APIs**
   - Swagger documentation
   - Standard HTTP methods

5. ✅ **Frontend Integration**
   - Angular app
   - Material UI
   - Real-time updates

### Các Tính Năng Có Thể Mở Rộng:

- ⏳ Authentication & Authorization (JWT)
- ⏳ Distributed Tracing
- ⏳ Service Discovery (Consul)
- ⏳ Circuit Breaker Pattern
- ⏳ Caching (Redis)
- ⏳ Monitoring & Logging

---

## 🎤 LỜI NÓI MẪU CHO DEMO

### Mở Đầu:
> "Hôm nay tôi sẽ demo một hệ thống E-Commerce được xây dựng theo kiến trúc Microservice. Hệ thống bao gồm 3 microservices chính: User Service, Product Service, và Order Service, tất cả được điều phối bởi một API Gateway."

### Khi Demo User Service:
> "Đầu tiên, chúng ta sẽ xem User Service. Service này quản lý tất cả thông tin người dùng và có database riêng của nó. Khi tôi tạo một user mới, request sẽ đi qua API Gateway, sau đó được route đến User Service, và cuối cùng được lưu vào PostgreSQL."

### Khi Demo Order Service:
> "Đây là phần quan trọng nhất - Order Service. Khi tạo một đơn hàng mới, không chỉ đơn hàng được lưu vào database, mà còn có một event được publish vào RabbitMQ. Điều này cho phép các services khác phản ứng với sự kiện này một cách bất đồng bộ, ví dụ như cập nhật tồn kho hoặc gửi thông báo."

### Kết Thúc:
> "Như các bạn thấy, kiến trúc Microservice cho phép chúng ta xây dựng các hệ thống linh hoạt, có khả năng mở rộng cao, và dễ bảo trì. Mỗi service có thể được phát triển, deploy, và scale độc lập."

---

## ⏱️ Timeline Tổng Thể

| Phần | Thời Gian | Mô Tả |
|------|-----------|-------|
| 1. Tổng quan | 2 phút | Giới thiệu kiến trúc |
| 2. User Service | 5 phút | CRUD operations |
| 3. Product Service | 5 phút | Quản lý sản phẩm |
| 4. Order Service | 8 phút | Tạo đơn hàng + RabbitMQ |
| 5. Kiến trúc | 3 phút | Database per service |
| 6. Tổng kết | 2 phút | Q&A |
| **TỔNG** | **25 phút** | |

---

## 🎯 Tips Cho Demo Thành Công

1. ✅ **Chuẩn bị dữ liệu mẫu trước:**
   - Tạo 2-3 users
   - Tạo 4-5 products
   - Tạo 1-2 orders

2. ✅ **Test trước khi demo:**
   - Đảm bảo tất cả services đang chạy
   - Test các APIs qua Swagger
   - Kiểm tra Frontend hoạt động

3. ✅ **Giải thích rõ ràng:**
   - Luồng request/response
   - Vai trò của từng component
   - Lợi ích của kiến trúc

4. ✅ **Xử lý lỗi:**
   - Nếu có lỗi, giải thích nguyên nhân
   - Show logs nếu cần
   - Có plan B (dùng Swagger nếu Frontend lỗi)

5. ✅ **Tương tác với audience:**
   - Hỏi câu hỏi
   - Để họ thử một số thao tác
   - Trả lời câu hỏi trong quá trình demo

---

## 📝 Checklist Trước Demo

- [ ] Tất cả backend services đang chạy
- [ ] Frontend đang chạy
- [ ] Databases đã được tạo
- [ ] Có dữ liệu mẫu sẵn
- [ ] Swagger UIs đều accessible
- [ ] RabbitMQ đang hoạt động
- [ ] Đã test các APIs chính
- [ ] Đã chuẩn bị lời nói
- [ ] Đã test timeline

**Chúc bạn demo thành công! 🚀**

