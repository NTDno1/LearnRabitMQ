import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignalRService } from './services/signalr.service';

interface Notification {
  id: number;
  message: string;
  timestamp: Date;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'SignalR Real-time Notifications';
  notifications: Notification[] = [];
  connectionStatus: 'connected' | 'disconnected' | 'connecting' = 'disconnected';
  private notificationIdCounter = 0;

  constructor(private signalRService: SignalRService) {}

  ngOnInit(): void {
    // Đăng ký lắng nghe sự kiện kết nối
    this.signalRService.connectionStatus$.subscribe(status => {
      this.connectionStatus = status;
    });

    // Đăng ký lắng nghe thông báo từ SignalR
    this.signalRService.notificationReceived$.subscribe(message => {
      this.addNotification(message);
    });

    // Tự động kết nối khi component khởi tạo
    this.connect();
  }

  ngOnDestroy(): void {
    // Ngắt kết nối khi component bị hủy
    this.disconnect();
  }

  /**
   * Kết nối tới SignalR Hub
   */
  connect(): void {
    this.connectionStatus = 'connecting';
    this.signalRService.startConnection()
      .then(() => {
        console.log('Đã kết nối thành công tới SignalR Hub');
      })
      .catch(error => {
        console.error('Lỗi kết nối SignalR:', error);
        this.connectionStatus = 'disconnected';
      });
  }

  /**
   * Ngắt kết nối khỏi SignalR Hub
   */
  disconnect(): void {
    this.signalRService.stopConnection();
  }

  /**
   * Thêm thông báo mới vào danh sách
   */
  private addNotification(message: string): void {
    const notification: Notification = {
      id: this.notificationIdCounter++,
      message: message,
      timestamp: new Date()
    };
    
    // Thêm vào đầu mảng để hiển thị thông báo mới nhất ở trên
    this.notifications.unshift(notification);

    // Giới hạn số lượng thông báo (tùy chọn)
    if (this.notifications.length > 50) {
      this.notifications = this.notifications.slice(0, 50);
    }
  }

  /**
   * Xóa tất cả thông báo
   */
  clearNotifications(): void {
    this.notifications = [];
  }

  /**
   * Lấy text hiển thị cho trạng thái kết nối
   */
  getStatusText(): string {
    switch (this.connectionStatus) {
      case 'connected':
        return 'Đã kết nối';
      case 'connecting':
        return 'Đang kết nối...';
      case 'disconnected':
        return 'Đã ngắt kết nối';
      default:
        return 'Không xác định';
    }
  }
}

