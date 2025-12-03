# 📘 Hướng dẫn chi tiết về Frontend Angular

Tài liệu này giải thích chi tiết từng phần của Frontend Angular cho người mới bắt đầu.

---

## 🎯 Tổng quan về Angular

Angular là một framework JavaScript/TypeScript để xây dựng ứng dụng web. Trong dự án này, chúng ta sử dụng **Angular Standalone Components** (không cần NgModule).

### Các khái niệm cơ bản:

1. **Component**: Một phần của UI (ví dụ: header, button, list)
2. **Service**: Logic nghiệp vụ, xử lý dữ liệu
3. **Template**: HTML hiển thị UI
4. **TypeScript**: Ngôn ngữ lập trình (JavaScript với types)

---

## 📁 Cấu trúc file Frontend

```
Frontend/
├── src/
│   ├── app/
│   │   ├── app.component.ts      # Logic của component
│   │   ├── app.component.html     # Template (HTML)
│   │   ├── app.component.css      # Styles (CSS)
│   │   └── services/
│   │       └── signalr.service.ts # Service quản lý SignalR
│   ├── main.ts                    # Entry point
│   ├── index.html                 # HTML chính
│   └── styles.css                 # Styles global
├── package.json                   # Dependencies
└── angular.json                   # Cấu hình Angular
```

---

## 🔍 Phân tích chi tiết từng file

### 1. `main.ts` - Entry Point

```typescript
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { provideHttpClient } from '@angular/common/http';

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient()]
}).catch(err => console.error(err));
```

**Giải thích:**

- `bootstrapApplication`: Khởi động ứng dụng Angular
- `AppComponent`: Component chính (root component)
- `provideHttpClient`: Cung cấp service để gọi HTTP API (tùy chọn, dùng cho tương lai)

**Khi nào chạy?**

- Khi bạn mở `http://localhost:4200`
- Angular sẽ load `index.html` → tìm `<app-root>` → load `AppComponent`

---

### 2. `app.component.ts` - Logic Component

#### 2.1. Import và Decorator

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignalRService } from './services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
```

**Giải thích:**

- `@Component`: Decorator đánh dấu đây là Angular Component
- `selector: 'app-root'`: Tên thẻ HTML (`<app-root></app-root>`)
- `standalone: true`: Component độc lập (không cần NgModule)
- `imports: [CommonModule]`: Import các directive như `*ngIf`, `*ngFor`
- `templateUrl`: Đường dẫn tới file HTML
- `styleUrl`: Đường dẫn tới file CSS

#### 2.2. Class và Properties

```typescript
export class AppComponent implements OnInit, OnDestroy {
  title = 'SignalR Real-time Notifications';
  notifications: Notification[] = [];
  connectionStatus: 'connected' | 'disconnected' | 'connecting' = 'disconnected';
  private notificationIdCounter = 0;

  constructor(private signalRService: SignalRService) {}
}
```

**Giải thích:**

- `implements OnInit, OnDestroy`: Implement lifecycle hooks
- `title`: Biến string
- `notifications`: Mảng chứa các thông báo
- `connectionStatus`: Trạng thái kết nối (union type)
- `notificationIdCounter`: Counter để tạo ID cho thông báo
- `constructor`: Inject `SignalRService` (Dependency Injection)

**Dependency Injection là gì?**

- Angular tự động tạo và cung cấp `SignalRService` khi tạo `AppComponent`
- Không cần `new SignalRService()` - Angular làm việc đó

#### 2.3. Lifecycle Hooks

```typescript
ngOnInit(): void {
  // Chạy khi component được khởi tạo
  this.signalRService.connectionStatus$.subscribe(status => {
    this.connectionStatus = status;
  });

  this.signalRService.notificationReceived$.subscribe(message => {
    this.addNotification(message);
  });

  this.connect();
}

ngOnDestroy(): void {
  // Chạy khi component bị hủy (đóng trang)
  this.disconnect();
}
```

**Giải thích:**

- `ngOnInit()`: Chạy **một lần** sau khi component được tạo
- `ngOnDestroy()`: Chạy **một lần** trước khi component bị hủy
- `subscribe()`: Đăng ký lắng nghe Observable (RxJS)

**Tại sao cần `ngOnDestroy()`?**

- Ngắt kết nối SignalR khi đóng trang
- Tránh memory leak (rò rỉ bộ nhớ)

#### 2.4. Methods

```typescript
connect(): void {
  this.connectionStatus = 'connecting';
  this.signalRService.startConnection()
    .then(() => {
      console.log('Đã kết nối thành công');
    })
    .catch(error => {
      console.error('Lỗi kết nối:', error);
      this.connectionStatus = 'disconnected';
    });
}
```

**Giải thích:**

- `connect()`: Method công khai, có thể gọi từ template
- `this.signalRService.startConnection()`: Gọi method của service
- `.then()`: Xử lý khi thành công (Promise)
- `.catch()`: Xử lý khi lỗi

```typescript
private addNotification(message: string): void {
  const notification: Notification = {
    id: this.notificationIdCounter++,
    message: message,
    timestamp: new Date()
  };
  
  this.notifications.unshift(notification);
}
```

**Giải thích:**

- `private`: Chỉ dùng trong class này (không gọi từ template)
- `unshift()`: Thêm vào **đầu** mảng (thông báo mới nhất ở trên)

---

### 3. `app.component.html` - Template

#### 3.1. Interpolation `{{ }}`

```html
<h1>{{ title }}</h1>
```

**Giải thích:**

- Hiển thị giá trị của biến `title`
- Angular tự động cập nhật khi `title` thay đổi

#### 3.2. Property Binding `[ ]`

```html
<button [disabled]="connectionStatus === 'connected'">
  Kết nối
</button>
```

**Giải thích:**

- `[disabled]`: Bind thuộc tính `disabled` của button
- Button bị vô hiệu hóa khi `connectionStatus === 'connected'`

#### 3.3. Event Binding `( )`

```html
<button (click)="connect()">Kết nối</button>
```

**Giải thích:**

- `(click)`: Lắng nghe sự kiện click
- Khi click → gọi method `connect()`

#### 3.4. Structural Directives

**`*ngIf` - Hiển thị/ẩn:**

```html
<div *ngIf="notifications.length === 0">
  Chưa có thông báo
</div>
```

**Giải thích:**

- `*ngIf`: Hiển thị element nếu điều kiện đúng
- Nếu `notifications.length === 0` → hiển thị div
- Nếu không → ẩn hoàn toàn (không render trong DOM)

**`*ngFor` - Lặp qua mảng:**

```html
<div *ngFor="let notification of notifications" class="notification-card">
  {{ notification.message }}
</div>
```

**Giải thích:**

- `*ngFor`: Lặp qua mảng `notifications`
- Với mỗi phần tử → tạo một `<div>`
- `let notification`: Biến đại diện cho phần tử hiện tại

**Ví dụ:**

Nếu `notifications = [
  { message: "Hello" },
  { message: "World" }
]`

→ Sẽ render 2 `<div>`:
```html
<div>Hello</div>
<div>World</div>
```

#### 3.5. Pipe `|`

```html
{{ notification.timestamp | date:'dd/MM/yyyy HH:mm:ss' }}
```

**Giải thích:**

- `| date`: Pipe format ngày tháng
- `'dd/MM/yyyy HH:mm:ss'`: Format string
- Ví dụ: `2024-01-15 10:30:00` → `15/01/2024 10:30:00`

---

### 4. `signalr.service.ts` - Service

#### 4.1. Injectable và Observable

```typescript
@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private notificationSubject = new Subject<string>();
  public notificationReceived$ = this.notificationSubject.asObservable();
}
```

**Giải thích:**

- `@Injectable`: Đánh dấu đây là service có thể inject
- `providedIn: 'root'`: Service singleton (một instance cho toàn app)
- `Subject`: RxJS Subject để phát giá trị
- `Observable`: Cho phép subscribe để nhận giá trị

**Subject vs Observable:**

- **Subject**: Có thể phát giá trị (`.next()`)
- **Observable**: Chỉ có thể subscribe để nhận giá trị

#### 4.2. Tạo Hub Connection

```typescript
this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect()
  .build();
```

**Giải thích:**

- `HubConnectionBuilder`: Builder pattern để tạo connection
- `withUrl()`: URL của SignalR Hub
- `withAutomaticReconnect()`: Tự động kết nối lại nếu mất kết nối
- `build()`: Tạo connection object

#### 4.3. Đăng ký Event Handler

```typescript
this.hubConnection.on('ReceiveNotification', (message: string) => {
  console.log('Received:', message);
  this.notificationSubject.next(message);
});
```

**Giải thích:**

- `.on()`: Đăng ký lắng nghe event từ Hub
- `'ReceiveNotification'`: Tên event (phải khớp với Backend)
- `(message: string) => {}`: Callback khi nhận event
- `this.notificationSubject.next(message)`: Phát giá trị tới Observable

**Luồng dữ liệu:**

```
Backend Hub → SignalR Client → Subject.next() → Observable → Component
```

#### 4.4. Start Connection

```typescript
return this.hubConnection
  .start()
  .then(() => {
    console.log('Connected');
    this.connectionStatusSubject.next('connected');
  })
  .catch(error => {
    console.error('Error:', error);
    throw error;
  });
```

**Giải thích:**

- `.start()`: Bắt đầu kết nối (trả về Promise)
- `.then()`: Xử lý khi thành công
- `.catch()`: Xử lý khi lỗi
- `throw error`: Ném lỗi để component có thể catch

---

## 🔄 Luồng hoạt động chi tiết

### Khi người dùng mở trang:

```
1. Browser load index.html
   ↓
2. Angular bootstrap AppComponent (main.ts)
   ↓
3. AppComponent.ngOnInit() chạy
   ↓
4. Gọi signalRService.startConnection()
   ↓
5. SignalR kết nối tới http://localhost:5000/notificationHub
   ↓
6. Đăng ký lắng nghe event "ReceiveNotification"
   ↓
7. Component subscribe Observable để nhận thông báo
   ↓
8. UI hiển thị trạng thái "Đã kết nối"
```

### Khi Backend gửi thông báo:

```
1. RabbitMQ Consumer nhận tin nhắn từ queue
   ↓
2. Consumer gọi hubContext.Clients.All.SendAsync("ReceiveNotification", message)
   ↓
3. SignalR Hub phát tới tất cả client đã kết nối
   ↓
4. SignalR Client (Angular) nhận event "ReceiveNotification"
   ↓
5. Service gọi notificationSubject.next(message)
   ↓
6. Component nhận giá trị qua Observable
   ↓
7. Component gọi addNotification(message)
   ↓
8. UI tự động cập nhật (Angular Change Detection)
```

---

## 🎨 Styling (CSS)

### Global Styles (`styles.css`)

```css
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
```

**Giải thích:**

- `*`: Selector cho tất cả elements
- Reset margin/padding về 0
- `box-sizing: border-box`: Padding và border tính trong width

### Component Styles (`app.component.css`)

- Styles trong file này chỉ áp dụng cho component này
- Không ảnh hưởng đến component khác

### Animation

```css
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.notification-card {
  animation: slideIn 0.3s ease-out;
}
```

**Giải thích:**

- `@keyframes`: Định nghĩa animation
- `from/to`: Trạng thái bắt đầu/kết thúc
- `animation`: Áp dụng animation cho element

---

## 🐛 Debugging Tips

### 1. Console Logs

Thêm `console.log()` để debug:

```typescript
connect(): void {
  console.log('Connecting...');
  this.signalRService.startConnection()
    .then(() => {
      console.log('Connected successfully');
    });
}
```

### 2. Developer Tools

- **F12**: Mở Developer Tools
- **Console**: Xem logs và errors
- **Network**: Kiểm tra HTTP requests
- **Elements**: Xem DOM structure

### 3. Angular DevTools

Cài đặt extension:
- Chrome: Angular DevTools
- Xem component tree, state, performance

---

## 📚 Tài liệu tham khảo

- [Angular Documentation](https://angular.io/docs)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [RxJS Documentation](https://rxjs.dev/)
- [SignalR JavaScript Client](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client)

---

## ❓ Câu hỏi thường gặp

### Q: Tại sao dùng Observable thay vì biến thường?

**A:** Observable cho phép:
- Nhiều component subscribe cùng một nguồn dữ liệu
- Tự động cập nhật khi có thay đổi
- Dễ dàng xử lý async operations

### Q: `standalone: true` là gì?

**A:** Angular mới (v14+) cho phép component hoạt động độc lập, không cần NgModule. Đơn giản hơn cho người mới.

### Q: Tại sao cần `ngOnDestroy()`?

**A:** Để cleanup:
- Unsubscribe Observable (tránh memory leak)
- Ngắt kết nối SignalR
- Hủy timers/intervals

---

**Chúc bạn học tốt! 🎉**

Test CICD