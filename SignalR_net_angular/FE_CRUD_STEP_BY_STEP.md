# 📘 Hướng dẫn tích hợp thêm view CRUD vào Frontend hiện tại (SignalR_net_angular)

Mục tiêu: thêm trang CRUD Items (API `/api/items`) trực tiếp trong dự án hiện tại, không tạo project mới.

## 0. Chuẩn bị
- Backend đã có `ItemsController` chạy ở `https://localhost:5001/api`.
- Frontend hiện tại đang bootstrap `AppComponent` (standalone).
- Cấu hình sẵn `environment.apiBaseUrl = https://localhost:5001/api`.

## 1) Model & Service (trong dự án hiện tại)
`src/app/models/item.model.ts`
```ts
export interface Item {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
}
```

`src/app/services/item.service.ts`
```ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Item } from '../models/item.model';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private api = environment.apiBaseUrl;
  constructor(private http: HttpClient) {}

  getAll(): Observable<Item[]> { return this.http.get<Item[]>(`${this.api}/items`); }
  get(id: number): Observable<Item> { return this.http.get<Item>(`${this.api}/items/${id}`); }
  create(payload: Partial<Item>): Observable<Item> { return this.http.post<Item>(`${this.api}/items`, payload); }
  update(id: number, payload: Partial<Item>): Observable<Item> { return this.http.put<Item>(`${this.api}/items/${id}`, payload); }
  delete(id: number): Observable<void> { return this.http.delete<void>(`${this.api}/items/${id}`); }
}
```

## 2) Tạo 3 component CRUD (standalone)
Chạy lệnh trong thư mục Frontend:
```bash
ng generate component app/components/item-list --standalone --flat --skip-tests
ng generate component app/components/item-form --standalone --flat --skip-tests
ng generate component app/components/items-page --standalone --flat --skip-tests
```

### ItemListComponent
`item-list.component.ts`
```ts
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Item } from '../models/item.model';

@Component({
  selector: 'app-item-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './item-list.component.html',
  styleUrls: ['./item-list.component.css']
})
export class ItemListComponent {
  @Input() items: Item[] = [];
  @Output() add = new EventEmitter<void>();
  @Output() edit = new EventEmitter<Item>();
  @Output() remove = new EventEmitter<Item>();
}
```

`item-list.component.html`
```html
<div class="d-flex justify-content-end mb-3">
  <button class="btn btn-primary" (click)="add.emit()">Add Product</button>
</div>

<table class="table table-bordered table-hover align-middle">
  <thead class="table-light">
    <tr>
      <th style="width:60px;">SN</th>
      <th>Product</th>
      <th>Description</th>
      <th style="width:220px;">Created</th>
      <th style="width:160px;">Action</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let item of items; let i = index">
      <td>{{ i + 1 }}</td>
      <td>{{ item.name }}</td>
      <td>{{ item.description }}</td>
      <td>{{ item.createdAt | date:'yyyy-MM-dd HH:mm:ss' }}</td>
      <td>
        <button class="btn btn-sm btn-info me-1" (click)="edit.emit(item)">✏️</button>
        <button class="btn btn-sm btn-danger" (click)="remove.emit(item)">🗑️</button>
      </td>
    </tr>
    <tr *ngIf="items.length === 0">
      <td colspan="5" class="text-center text-muted">No data</td>
    </tr>
  </tbody>
</table>
```

`item-list.component.css`
```css
table th, table td { vertical-align: middle; }
```

### ItemFormComponent
`item-form.component.ts`
```ts
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Item } from '../models/item.model';

@Component({
  selector: 'app-item-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './item-form.component.html',
  styleUrls: ['./item-form.component.css']
})
export class ItemFormComponent implements OnChanges {
  @Input() item?: Item;
  @Output() save = new EventEmitter<Partial<Item>>();
  @Output() cancel = new EventEmitter<void>();

  form: Partial<Item> = { name: '', description: '' };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['item']) {
      this.form = this.item ? { ...this.item } : { name: '', description: '' };
    }
  }

  submit(): void {
    if (!this.form.name || this.form.name.trim() === '') return;
    this.save.emit({
      name: this.form.name?.trim(),
      description: this.form.description?.trim()
    });
  }
}
```

`item-form.component.html`
```html
<div class="card">
  <div class="card-body">
    <h5 class="card-title mb-3">{{ item ? 'Edit Product' : 'Add Product' }}</h5>
    <div class="mb-3">
      <label class="form-label">Product Name</label>
      <input class="form-control" [(ngModel)]="form.name" placeholder="Enter name">
    </div>
    <div class="mb-3">
      <label class="form-label">Description</label>
      <textarea class="form-control" rows="3" [(ngModel)]="form.description" placeholder="Enter description"></textarea>
    </div>
    <div class="d-flex gap-2">
      <button class="btn btn-primary" (click)="submit()">{{ item ? 'Update' : 'Add' }}</button>
      <button class="btn btn-secondary" type="button" (click)="cancel.emit()">Cancel</button>
    </div>
  </div>
</div>
```

`item-form.component.css`
```css
.card { box-shadow: 0 4px 10px rgba(0, 0, 0, 0.05); }
```

### ItemsPageComponent (container)
`items-page.component.ts`
```ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Item } from '../models/item.model';
import { ItemService } from '../services/item.service';
import { ItemListComponent } from './item-list.component';
import { ItemFormComponent } from './item-form.component';

@Component({
  selector: 'app-items-page',
  standalone: true,
  imports: [CommonModule, ItemListComponent, ItemFormComponent],
  templateUrl: './items-page.component.html',
  styleUrls: ['./items-page.component.css']
})
export class ItemsPageComponent implements OnInit {
  items: Item[] = [];
  selected: Item | null = null;
  showForm = false;
  loading = false;

  constructor(private itemService: ItemService) {}
  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.itemService.getAll().subscribe({
      next: (res) => { this.items = res; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }
  onAddClick(): void { this.selected = null; this.showForm = true; }
  onEdit(item: Item): void { this.selected = item; this.showForm = true; }
  onRemove(item: Item): void {
    if (!confirm('Delete this item?')) return;
    this.itemService.delete(item.id).subscribe(() => {
      this.items = this.items.filter(x => x.id !== item.id);
    });
  }
  onSave(payload: Partial<Item>): void {
    if (this.selected) {
      this.itemService.update(this.selected.id, payload).subscribe(updated => {
        this.items = this.items.map(x => x.id === updated.id ? updated : x);
        this.resetForm();
      });
    } else {
      this.itemService.create(payload).subscribe(created => {
        this.items = [created, ...this.items];
        this.resetForm();
      });
    }
  }
  onCancel(): void { this.resetForm(); }
  private resetForm(): void { this.selected = null; this.showForm = false; }
}
```

`items-page.component.html`
```html
<div class="container py-4">
  <h2 class="mb-4">CRUD Operations</h2>

  <app-item-list
    [items]="items"
    (add)="onAddClick()"
    (edit)="onEdit($event)"
    (remove)="onRemove($event)">
  </app-item-list>

  <div class="mt-4" *ngIf="showForm">
    <app-item-form
      [item]="selected"
      (save)="onSave($event)"
      (cancel)="onCancel()">
    </app-item-form>
  </div>
</div>
```

`items-page.component.css`
```css
.container { max-width: 1100px; }
```

## 3) Thêm route mới vào app hiện tại
Tạo (hoặc cập nhật) `src/app/app.routes.ts`:
```ts
import { Routes } from '@angular/router';
import { ItemsPageComponent } from './components/items-page.component';
import { ChatComponent } from './components/chat.component';

export const routes: Routes = [
  { path: 'items', component: ItemsPageComponent }, // view CRUD mới
  { path: 'chat', component: ChatComponent },       // view chat cũ
  { path: '', redirectTo: 'items', pathMatch: 'full' }
];
```

`src/main.ts` chỉnh để cung cấp router (giữ HttpClient):
```ts
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient(), provideRouter(routes)]
}).catch(err => console.error(err));
```

`src/app/app.component.ts` đổi sang dùng RouterOutlet:
```ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {}
```

`src/app/app.component.html` thêm điều hướng:
```html
<div class="app-container">
  <nav class="p-3 border-bottom mb-3">
    <a class="me-3" routerLink="/items">Items CRUD</a>
    <a class="me-3" routerLink="/chat">Chat</a>
  </nav>
  <router-outlet></router-outlet>
</div>
```
> Nếu muốn giữ logic đăng nhập cho Chat, có thể ẩn link Chat khi chưa login hoặc thêm guard.

## 4) Chạy thử
Backend:
```bash
cd SignalR_net_angular/Backend
dotnet run
```
Frontend:
```bash
cd SignalR_net_angular/Frontend
npm install
ng serve
```
Mở `http://localhost:4200/items` → Thêm/Sửa/Xóa; kiểm tra Network gọi `https://localhost:5001/api/items`.

## 5) Luồng FE (tóm tắt)
- `ItemsPageComponent` giữ state và gọi `ItemService`.
- `ItemListComponent` render bảng, emit Add/Edit/Delete.
- `ItemFormComponent` render form, emit Save/Cancel.
- Add: showForm=true → create → prepend item.
- Edit: set selected → update → replace item.
- Delete: confirm → delete → filter item.

## 6) Lưu ý
- CORS: BE đã bật policy `AllowAngular`; FE gọi HTTPS 5001. Nếu lỗi cert dev, trust cert .NET hoặc dùng `--ssl false`.
- Phân trang: hiện client-side đơn giản; muốn server-side thì thêm query `page/pageSize` ở BE/FE.

