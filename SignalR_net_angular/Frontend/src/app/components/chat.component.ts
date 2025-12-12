import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, MessageDto, UserDto, ConversationDto } from '../services/chat.service';
import { AuthService, AuthResponse } from '../services/auth.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit, OnDestroy {
  currentUser: AuthResponse | null = null;
  conversations: ConversationDto[] = [];
  users: UserDto[] = [];
  selectedConversation: ConversationDto | null = null;
  messages: MessageDto[] = [];
  newMessage = '';

  // Mongo view (riêng)
  mongoSelectedUser: UserDto | null = null;
  mongoMessages: MessageDto[] = [];
  mongoNewMessage = '';
  mongoLoading = false;

  searchQuery = '';
  showUserList = false;
  isLoading = false;

  constructor(
    private chatService: ChatService,
    private authService: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    this.currentUser = this.authService.getCurrentUser();
    if (!this.currentUser) {
      return;
    }

    // Kết nối SignalR
    try {
      await this.chatService.startConnection();
      this.setupSignalRHandlers();
    } catch (error) {
      console.error('Lỗi kết nối SignalR:', error);
    }

    // Load conversations và users
    await this.loadConversations();
    await this.loadUsers();
  }

  ngOnDestroy(): void {
    this.chatService.stopConnection();
  }

  private setupSignalRHandlers(): void {
    // Nhận tin nhắn mới
    this.chatService.messageReceived$.subscribe(message => {
      if (this.selectedConversation && 
          (message.senderId === this.selectedConversation.otherUserId || 
           message.receiverId === this.selectedConversation.otherUserId)) {
        this.messages.push(message);
        this.scrollToBottom();
      }
      // Reload conversations để cập nhật last message
      this.loadConversations();
    });

    // Tin nhắn do chính mình gửi (SQL/SignalR) -> hiển thị ngay
    this.chatService.messageSent$.subscribe(message => {
      if (this.selectedConversation &&
          (message.receiverId === this.selectedConversation.otherUserId || message.senderId === this.selectedConversation.otherUserId)) {
        if (!this.existsMessage(this.messages, message)) {
          this.messages.push(message);
          this.scrollToBottom();
        }
      }
    });

    // User online/offline
    this.chatService.userOnline$.subscribe(userId => {
      const user = this.users.find(u => u.id === userId);
      if (user) {
        user.isOnline = true;
      }
    });

    this.chatService.userOffline$.subscribe(userId => {
      const user = this.users.find(u => u.id === userId);
      if (user) {
        user.isOnline = false;
      }
    });

    // Mongo realtime: nhận tin nhắn vào Mongo view
    this.chatService.mongoMessageReceived$.subscribe(message => {
      if (this.mongoSelectedUser &&
          (message.senderId === this.mongoSelectedUser.id || message.receiverId === this.mongoSelectedUser.id)) {
        if (!this.existsMessage(this.mongoMessages, message)) {
          this.mongoMessages.push(message);
        }
        this.scrollToBottomMongo();
      }
    });

    // Mongo realtime: xác nhận tin do chính mình gửi
    this.chatService.mongoMessageSent$.subscribe(message => {
      if (this.mongoSelectedUser &&
          (message.receiverId === this.mongoSelectedUser.id || message.senderId === this.mongoSelectedUser.id)) {
        if (!this.existsMessage(this.mongoMessages, message)) {
          this.mongoMessages.push(message);
        }
        this.scrollToBottomMongo();
      }
    });
  }

  async loadConversations(): Promise<void> {
    try {
      const response = await this.chatService.getConversations().toPromise();
      if (response?.success) {
        this.conversations = response.data;
      }
    } catch (error) {
      console.error('Lỗi load conversations:', error);
    }
  }

  async loadUsers(): Promise<void> {
    try {
      const response = await this.chatService.getUsers(this.searchQuery).toPromise();
      if (response?.success) {
        this.users = response.data;
      }
    } catch (error) {
      console.error('Lỗi load users:', error);
    }
  }

  async selectConversation(conversation: ConversationDto): Promise<void> {
    this.selectedConversation = conversation;
    this.isLoading = true;

    try {
      const response = await this.chatService.getMessages(conversation.otherUserId).toPromise();
      if (response?.success) {
        this.messages = response.data;
        this.scrollToBottom();
      }
    } catch (error) {
      console.error('Lỗi load messages:', error);
    } finally {
      this.isLoading = false;
    }
  }

  async startChatWithUser(user: UserDto): Promise<void> {
    // Tìm conversation với user này
    let conversation = this.conversations.find(c => c.otherUserId === user.id);
    
    if (!conversation) {
      // Tạo conversation mới
      conversation = {
        id: 0,
        otherUserId: user.id,
        otherUsername: user.username,
        otherDisplayName: user.displayName,
        otherAvatarUrl: user.avatarUrl,
        otherIsOnline: user.isOnline,
        lastMessageAt: new Date(),
        unreadCount: 0
      };
      this.conversations.unshift(conversation);
    }

    await this.selectConversation(conversation);
    this.showUserList = false;
  }

  async sendMessage(): Promise<void> {
    if (!this.newMessage.trim() || !this.selectedConversation) {
      return;
    }

    const content = this.newMessage.trim();
    this.newMessage = '';

    try {
      await this.chatService.sendMessage(this.selectedConversation.otherUserId, content);
      // Message sẽ được thêm vào list qua SignalR event
    } catch (error) {
      console.error('Lỗi gửi tin nhắn:', error);
      // Fallback: gửi qua API
      try {
        const response = await this.chatService.sendMessageViaApi(
          this.selectedConversation.otherUserId,
          content
        ).toPromise();
        if (response?.success) {
          this.messages.push(response.data);
          this.scrollToBottom();
        }
      } catch (apiError) {
        console.error('Lỗi gửi tin nhắn qua API:', apiError);
        alert('Không thể gửi tin nhắn. Vui lòng thử lại.');
      }
    }
  }

  async onSearchChange(): Promise<void> {
    await this.loadUsers();
  }

  logout(): void {
    this.authService.logout();
    window.location.reload();
  }

  getDisplayName(user: UserDto | ConversationDto): string {
    if (this.isConversation(user)) {
      return user.otherDisplayName || user.otherUsername;
    }
    return user.displayName || user.username;
  }

  private isConversation(obj: UserDto | ConversationDto): obj is ConversationDto {
    return (obj as ConversationDto).otherUserId !== undefined;
  }

  formatTime(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    const now = new Date();
    const diff = now.getTime() - d.getTime();
    const minutes = Math.floor(diff / 60000);

    if (minutes < 1) return 'Vừa xong';
    if (minutes < 60) return `${minutes} phút trước`;
    if (minutes < 1440) return `${Math.floor(minutes / 60)} giờ trước`;
    return d.toLocaleDateString('vi-VN');
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const messagesContainer = document.querySelector('.messages-container');
      if (messagesContainer) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
      }
    }, 100);
  }

  private scrollToBottomMongo(): void {
    setTimeout(() => {
      const container = document.querySelector('.mongo-chat-box .messages-container');
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    }, 100);
  }

  // -------- Mongo Chat (view riêng, không dùng SignalR) --------
  async selectMongoUser(user: UserDto): Promise<void> {
    this.mongoSelectedUser = user;
    this.mongoLoading = true;
    try {
      const response = await this.chatService.getMongoConversation(user.id).toPromise();
      if (response?.success) {
        this.mongoMessages = response.data;
      }
    } catch (error) {
      console.error('Lỗi load lịch sử Mongo:', error);
    } finally {
      this.mongoLoading = false;
    }
  }

  async sendMongoMessage(): Promise<void> {
    if (!this.mongoNewMessage.trim() || !this.mongoSelectedUser) return;
    const content = this.mongoNewMessage.trim();
    this.mongoNewMessage = '';
    try {
      await this.chatService.sendMessageMongo(
        this.mongoSelectedUser.id,
        content
      ).toPromise();
      // Tin nhắn sẽ được đẩy về qua SignalR (MongoMessageSent)
    } catch (error) {
      console.error('Lỗi gửi tin nhắn Mongo:', error);
      alert('Không thể gửi tin nhắn Mongo. Vui lòng thử lại.');
    }
  }

  private existsMessage(list: MessageDto[], message: MessageDto): boolean {
    return list.some(m =>
      m.id !== 0 && m.id === message.id ||
      (m.id === 0 && message.id === 0 &&
       m.senderId === message.senderId &&
       m.receiverId === message.receiverId &&
       m.content === message.content &&
       new Date(m.sentAt).getTime() === new Date(message.sentAt).getTime())
    );
  }
}

