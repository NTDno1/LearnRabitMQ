# 🚀 Hướng dẫn nhanh (Quick Start)

## Backend

```bash
cd Backend
dotnet restore
dotnet run
```

Backend chạy tại: `http://localhost:5000`

## Frontend

```bash
cd Frontend
npm install
ng serve
```

Frontend chạy tại: `http://localhost:4200`

## Test

1. Mở `http://localhost:4200` trong trình duyệt
2. Click nút "Kết nối"
3. Mở `http://localhost:5000/swagger`
4. Gọi API `POST /api/Test/send-message` với body: `"Test message"`
5. Kiểm tra Frontend - thông báo sẽ xuất hiện!

## Cấu hình

- RabbitMQ: `47.130.33.106:5672` (guest/guest)
- PostgreSQL: `47.130.33.106:5432` (postgres/123456)

Xem `README.md` để biết chi tiết!

