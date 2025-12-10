import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  
  // Subject để phát thông báo mới tới các component đăng ký
  private notificationSubject = new Subject<string>();
  public notificationReceived$ = this.notificationSubject.asObservable();

  // BehaviorSubject để theo dõi trạng thái kết nối
  private connectionStatusSubject = new BehaviorSubject<'connected' | 'disconnected' | 'connecting'>('disconnected');
  public connectionStatus$ = this.connectionStatusSubject.asObservable();

  /**
   * Khởi tạo và bắt đầu kết nối tới SignalR Hub
   */
  public startConnection(): Promise<void> {
    // URL của SignalR Hub (thay đổi nếu backend chạy ở port khác)
    const hubUrl = environment.notificationHubUrl;

    // Tạo Hub Connection
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect() // Tự động kết nối lại nếu mất kết nối
      .build();

    // Đăng ký lắng nghe sự kiện từ Hub
    this.registerOnServerEvents();

    // Bắt đầu kết nối
    this.connectionStatusSubject.next('connecting');
    
    return this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connection Started');
        this.connectionStatusSubject.next('connected');
      })
      .catch(error => {
        console.error('Error starting SignalR connection:', error);
        this.connectionStatusSubject.next('disconnected');
        throw error;
      });
  }

  /**
   * Đăng ký các event handler để lắng nghe tin nhắn từ Hub
   */
  private registerOnServerEvents(): void {
    if (!this.hubConnection) {
      return;
    }

    // Lắng nghe event "ReceiveNotification" từ Hub
    // Event này được gọi từ RabbitMQConsumerService khi nhận tin nhắn từ RabbitMQ
    this.hubConnection.on('ReceiveNotification', (message: string) => {
      console.log('Received notification from Hub:', message);
      // Phát thông báo tới tất cả component đã subscribe
      this.notificationSubject.next(message);
    });

    // Lắng nghe event "ReceiveMessage" (tùy chọn, dùng để test)
    this.hubConnection.on('ReceiveMessage', (user: string, message: string) => {
      console.log(`Message from ${user}: ${message}`);
      this.notificationSubject.next(`[${user}]: ${message}`);
    });

    // Lắng nghe sự kiện kết nối lại
    this.hubConnection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
      this.connectionStatusSubject.next('connecting');
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.connectionStatusSubject.next('connected');
    });

    this.hubConnection.onclose(() => {
      console.log('SignalR connection closed');
      this.connectionStatusSubject.next('disconnected');
    });
  }

  /**
   * Dừng kết nối tới Hub
   */
  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection
        .stop()
        .then(() => {
          console.log('SignalR Connection Stopped');
          this.connectionStatusSubject.next('disconnected');
        })
        .catch(error => {
          console.error('Error stopping SignalR connection:', error);
        });
    }
  }

  /**
   * Gửi tin nhắn tới Hub (tùy chọn, dùng để test)
   */
  public sendMessage(user: string, message: string): Promise<void> {
    if (!this.hubConnection || this.connectionStatusSubject.value !== 'connected') {
      return Promise.reject('Connection is not established');
    }

    return this.hubConnection.invoke('SendMessage', user, message)
      .catch(error => {
        console.error('Error sending message:', error);
        throw error;
      });
  }
}

