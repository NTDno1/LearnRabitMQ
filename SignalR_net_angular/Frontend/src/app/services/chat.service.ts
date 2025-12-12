import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { AuthService } from './auth.service';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface MessageDto {
  id: number;
  conversationId: number;
  senderId: number;
  senderUsername: string;
  senderDisplayName?: string;
  receiverId: number;
  receiverUsername: string;
  content: string;
  sentAt: Date;
  isRead: boolean;
}

export interface UserDto {
  id: number;
  username: string;
  displayName?: string;
  avatarUrl?: string;
  isOnline: boolean;
  lastSeen?: Date;
}

export interface ConversationDto {
  id: number;
  otherUserId: number;
  otherUsername: string;
  otherDisplayName?: string;
  otherAvatarUrl?: string;
  otherIsOnline: boolean;
  lastMessage?: MessageDto;
  lastMessageAt: Date;
  unreadCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = environment.apiBaseUrl;
  private hubConnection?: signalR.HubConnection;
  
  // Observables
  private messageReceivedSubject = new Subject<MessageDto>();
  public messageReceived$ = this.messageReceivedSubject.asObservable();
  
  private messageSentSubject = new Subject<MessageDto>();
  public messageSent$ = this.messageSentSubject.asObservable();
  
  private userOnlineSubject = new Subject<number>();
  public userOnline$ = this.userOnlineSubject.asObservable();
  
  private userOfflineSubject = new Subject<number>();
  public userOffline$ = this.userOfflineSubject.asObservable();

  // Mongo realtime
  private mongoMessageReceivedSubject = new Subject<MessageDto>();
  public mongoMessageReceived$ = this.mongoMessageReceivedSubject.asObservable();

  private mongoMessageSentSubject = new Subject<MessageDto>();
  public mongoMessageSent$ = this.mongoMessageSentSubject.asObservable();

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token || ''}`,
      'Content-Type': 'application/json'
    });
  }

  /**
   * Kết nối tới ChatHub
   */
  startConnection(): Promise<void> {
    const token = this.authService.getToken();
    if (!token) {
      return Promise.reject('Chưa đăng nhập');
    }

    const hubUrl = `${environment.chatHubUrl}?token=${encodeURIComponent(token)}`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    // Đăng ký event handlers
    this.registerHandlers();

    return this.hubConnection.start();
  }

  /**
   * Đăng ký các event handlers
   */
  private registerHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMessage', (message: MessageDto) => {
      this.messageReceivedSubject.next(message);
    });

    this.hubConnection.on('MessageSent', (message: MessageDto) => {
      this.messageSentSubject.next(message);
    });

    this.hubConnection.on('UserOnline', (userId: number) => {
      this.userOnlineSubject.next(userId);
    });

    this.hubConnection.on('UserOffline', (userId: number) => {
      this.userOfflineSubject.next(userId);
    });

    this.hubConnection.on('MessageRead', (messageId: number) => {
      // Có thể emit event nếu cần
    });

    // Mongo realtime
    this.hubConnection.on('ReceiveMongoMessage', (message: MessageDto) => {
      this.mongoMessageReceivedSubject.next(message);
    });

    this.hubConnection.on('MongoMessageSent', (message: MessageDto) => {
      this.mongoMessageSentSubject.next(message);
    });

    this.hubConnection.on('Error', (error: string) => {
      console.error('ChatHub error:', error);
    });
  }

  /**
   * Ngắt kết nối
   */
  stopConnection(): void {
    this.hubConnection?.stop();
  }

  /**
   * Gửi tin nhắn qua SignalR
   */
  sendMessage(receiverId: number, content: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Chưa kết nối tới ChatHub');
    }

    return this.hubConnection.invoke('SendMessage', receiverId, content);
  }

  /**
   * Đánh dấu tin nhắn đã đọc
   */
  markAsRead(messageId: number): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Chưa kết nối tới ChatHub');
    }

    return this.hubConnection.invoke('MarkAsRead', messageId);
  }

  /**
   * API: Lấy danh sách users
   */
  getUsers(search?: string): Observable<any> {
    let params = new HttpParams();
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<any>(`${this.apiUrl}/Messages/users`, {
      headers: this.getHeaders(),
      params
    });
  }

  /**
   * API: Lấy danh sách conversations
   */
  getConversations(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/Messages/conversations`, {
      headers: this.getHeaders()
    });
  }

  /**
   * API: Lấy lịch sử chat lưu trên MongoDB
   */
  getMongoHistory(otherUserId: number, limit: number = 100): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/Messages/mongo/history/${otherUserId}`, {
      headers: this.getHeaders(),
      params: { limit: limit.toString() }
    });
  }

  /**
   * API: Lấy tin nhắn trong conversation
   */
  getMessages(otherUserId: number, page: number = 1, pageSize: number = 50): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/Messages/conversation/${otherUserId}`, {
      headers: this.getHeaders(),
      params: { page: page.toString(), pageSize: pageSize.toString() }
    });
  }

  /**
   * API: Gửi tin nhắn (backup nếu SignalR fail)
   */
  sendMessageViaApi(receiverId: number, content: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Messages/send`, {
      receiverId,
      content
    }, {
      headers: this.getHeaders()
    });
  }

  /**
   * API: Đánh dấu đã đọc
   */
  markAsReadViaApi(messageId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Messages/read/${messageId}`, {}, {
      headers: this.getHeaders()
    });
  }

  /**
   * API: Gửi tin nhắn và lưu trực tiếp MongoDB
   */
  sendMessageMongo(receiverId: number, content: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Messages/mongo/send`, {
      receiverId,
      content
    }, {
      headers: this.getHeaders()
    });
  }

  /**
   * API: Lấy lịch sử Mongo (conversation)
   */
  getMongoConversation(otherUserId: number, limit: number = 100): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/Messages/mongo/history/${otherUserId}`, {
      headers: this.getHeaders(),
      params: { limit: limit.toString() }
    });
  }
}

