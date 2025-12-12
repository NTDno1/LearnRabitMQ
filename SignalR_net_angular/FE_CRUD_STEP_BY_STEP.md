# 📘 Hướng dẫn từng bước (Front-end Angular) xây dựng CRUD Items

Mục tiêu: tự tay xây dựng giao diện CRUD (giống bảng mẫu) gọi API đã có sẵn ở Backend `/api/items`.

## 0. Chuẩn bị
- Node 18+, Angular CLI 17+.
- Backend đang chạy ở `https://localhost:5001/api`.
- Dùng SCSS hoặc CSS đều được; ví dụ bên dưới dùng SCSS.

## 1. Tạo dự án Angular
```bash
ng new crud-app --routing --style=scss
cd crud-app
npm install
```

## 2. Cấu hình environment
`src/environments/environment.ts`
```ts
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:5001/api'
};
```

## 3. Tạo model
`src/app/models/item.model.ts`
```ts
export interface Item {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
}
```

## 4. Tạo service gọi API
```bash
ng generate service services/item
```
`src/app/services/item.service.ts`
```ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Item } from '../models/item.model';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private api = environment.apiBaseUrl;
  constructor(private http: HttpClient) {}

  getAll(): Observable<Item[]> {
    return this.http.get<Item[]>(`${this.api}/items`);
  }
  get(id: number): Observable<Item> {
    return this.http.get<Item>(`${this.api}/items/${id}`);
  }
  create(payload: Partial<Item>): Observable<Item> {
    return this.http.post<Item>(`${this.api}/items`, payload);
  }
  update(id: number, payload: Partial<Item>): Observable<Item> {
    return this.http.put<Item>(`${this.api}/items/${id}`, payload);
  }
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/items/${id}`);
  }
}
```

## 5. Cài Bootstrap để làm UI nhanh
```bash
npm install bootstrap
```
`src/styles.scss`
```scss
@import "bootstrap/dist/css/bootstrap.min.css";
```

## 6. Tạo components
Chúng ta dùng 3 component standalone: `ItemsPage`, `ItemList`, `ItemForm`.

```bash
ng generate component pages/items-page --standalone --flat --skip-tests
ng generate component components/item-list --standalone --flat --skip-tests
ng generate component components/item-form --standalone --flat --skip-tests
```

### 6.1 ItemListComponent
Chức năng: hiển thị bảng + nút Add, Edit, Delete.

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
  styleUrls: ['./item-list.component.scss']
})
export class ItemListComponent {
  @Input() items: Item[] = [];
  @Output() add = new EventEmitter<void>();
  @Output() edit = new EventEmitter<Item>();
  @Output() remove = new EventEmitter<Item>();
}
```

`item-list.component.html` (bố cục giống hình mẫu)
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

`item-list.component.scss`
```scss
table th, table td {
  vertical-align: middle;
}
```

### 6.2 ItemFormComponent
Chức năng: form Add/Edit.

`item-form.component.ts`
```ts
import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Item } from '../models/item.model';

@Component({
  selector: 'app-item-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './item-form.component.html',
  styleUrls: ['./item-form.component.scss']
})
export class ItemFormComponent implements OnChanges {
  @Input() item?: Item;          // nếu có => edit mode
  @Output() save = new EventEmitter<Partial<Item>>();
  @Output() cancel = new EventEmitter<void>();

  form: Partial<Item> = { name: '', description: '' };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['item']) {
      this.form = this.item ? { ...this.item } : { name: '', description: '' };
    }
  }

  submit() {
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
      <button class="btn btn-primary" (click)="submit()">
        {{ item ? 'Update' : 'Add' }}
      </button>
      <button class="btn btn-secondary" type="button" (click)="cancel.emit()">Cancel</button>
    </div>
  </div>
</div>
```

### 6.3 ItemsPage (container)
Chức năng: giữ state, gọi service, điều khiển list + form.

`items-page.component.ts`
```ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Item } from '../models/item.model';
import { ItemService } from '../services/item.service';
import { ItemListComponent } from '../components/item-list.component';
import { ItemFormComponent } from '../components/item-form.component';

@Component({
  selector: 'app-items-page',
  standalone: true,
  imports: [CommonModule, ItemListComponent, ItemFormComponent],
  templateUrl: './items-page.component.html',
  styleUrls: ['./items-page.component.scss']
})
export class ItemsPageComponent implements OnInit {
  items: Item[] = [];
  selected: Item | null = null;
  showForm = false;
  loading = false;

  constructor(private itemService: ItemService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.itemService.getAll().subscribe({
      next: (res) => { this.items = res; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  onAddClick(): void {
    this.selected = null;
    this.showForm = true;
  }

  onEdit(item: Item): void {
    this.selected = item;
    this.showForm = true;
  }

  onRemove(item: Item): void {
    if (!confirm('Delete this item?')) return;
    this.itemService.delete(item.id).subscribe(() => {
      this.items = this.items.filter(x => x.id !== item.id);
    });
  }

  onSave(payload: Partial<Item>): void {
    if (this.selected) {
      // update
      this.itemService.update(this.selected.id, payload).subscribe(updated => {
        this.items = this.items.map(x => x.id === updated.id ? updated : x);
        this.resetForm();
      });
    } else {
      // create
      this.itemService.create(payload).subscribe(created => {
        this.items = [created, ...this.items];
        this.resetForm();
      });
    }
  }

  onCancel(): void {
    this.resetForm();
  }

  private resetForm(): void {
    this.selected = null;
    this.showForm = false;
  }
}
```

`items-page.component.html`
```html
<div class="container py-4">
  <h2 class="mb-4">CRUD Operations in Angular</h2>

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

## 7. Routing
`src/app/app.routes.ts`
```ts
import { Routes } from '@angular/router';
import { ItemsPageComponent } from './pages/items-page.component';

export const routes: Routes = [
  { path: 'items', component: ItemsPageComponent },
  { path: '', redirectTo: 'items', pathMatch: 'full' }
];
```

`src/main.ts` (mặc định Angular 17 đã cấu hình, chỉ cần đảm bảo import routes):
```ts
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { provideHttpClient } from '@angular/common/http';

bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes), provideHttpClient()]
});
```

`src/app/app.component.ts` giữ `<router-outlet></router-outlet>` hoặc nếu vẫn dùng Login/Chat cũ thì tạo app shell mới cho CRUD.

## 8. Chạy thử
Backend (đã có sẵn):
```bash
cd SignalR_net_angular/Backend
dotnet run
```
Frontend:
```bash
cd crud-app
npm install
ng serve
```
Mở `http://localhost:4200/items` → Thêm / Sửa / Xóa → kiểm tra Network gọi `https://localhost:5001/api/items`.

## 9. Giải thích luồng FE (tóm tắt)
- `ItemsPageComponent` giữ state, gọi `ItemService`.
- `ItemListComponent` chỉ render bảng và emit sự kiện Add/Edit/Delete.
- `ItemFormComponent` hiển thị form, emit `save(payload)` và `cancel()`.
- Dòng chảy: 
  - `Add` → `showForm = true` → submit → `ItemService.create()` → prepend vào `items`.
  - `Edit` → set `selected` → form patch → submit → `ItemService.update()` → replace item trong `items`.
  - `Delete` → confirm → `ItemService.delete()` → filter item khỏi `items`.

## 10. Lưu ý
- Nếu CORS: đảm bảo BE đã bật CORS cho FE (trong BE Program.cs đã có policy `AllowAngular`).
- SSL: FE gọi `https://localhost:5001`; nếu gặp lỗi cert dev, chạy FE với `--ssl false` hoặc trust cert dev của .NET.
- Phân trang: ví dụ trên là client-side đơn giản; nếu muốn server-side, thêm query `page/pageSize` và cập nhật API.

