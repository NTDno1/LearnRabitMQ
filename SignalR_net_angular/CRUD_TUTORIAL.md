# 🧭 Hướng dẫn CRUD cơ bản (BE có sẵn, FE tự làm theo)

## Phần 1: Back-end (ASP.NET Core + PostgreSQL)

### 1. Khởi tạo dự án
```bash
dotnet new webapi -n CrudApi
cd CrudApi
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### 2. Model & DbContext
`Models/Item.cs`
```csharp
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

`Data/AppDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using CrudApi.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Item> Items => Set<Item>();
}
```

### 3. Cấu hình kết nối & Program.cs
`appsettings.json` (sửa connection string cho PostgreSQL)
```json
"ConnectionStrings": {
  "PostgreSQL": "Host=localhost;Port=5432;Database=crud_db;Username=postgres;Password=yourpassword"
}
```

`Program.cs`
```csharp
using CrudApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Tự migrate DB khi khởi động (dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
```

### 4. Controller CRUD
`Controllers/ItemsController.cs`
```csharp
using CrudApi.Data;
using CrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ItemsController(AppDbContext db) => _db = db;

    [HttpGet] // GET /api/items
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Items.OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpGet("{id}")] // GET /api/items/{id}
    public async Task<IActionResult> Get(int id)
    {
        var item = await _db.Items.FindAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost] // POST /api/items
    public async Task<IActionResult> Create([FromBody] Item item)
    {
        item.Id = 0;
        item.CreatedAt = DateTime.UtcNow;
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id}")] // PUT /api/items/{id}
    public async Task<IActionResult> Update(int id, [FromBody] Item dto)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();
        item.Name = dto.Name;
        item.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")] // DELETE /api/items/{id}
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
```

### 5. Migrate DB
```bash
dotnet tool install --global dotnet-ef   # nếu chưa có
dotnet ef migrations add InitItems
dotnet ef database update
```

### 6. Chạy API
```bash
dotnet run
```
Swagger: `https://localhost:5001/swagger`
Endpoints:  
- GET /api/items  
- POST /api/items  
- GET /api/items/{id}  
- PUT /api/items/{id}  
- DELETE /api/items/{id}  

---

## Phần 2: Front-end (Angular) – Hướng dẫn Step-by-Step (tự làm)

### Bước 0: Chuẩn bị
- Node 18+, Angular CLI 17+
- API đã chạy ở `https://localhost:5001/api`

### Bước 1: Khởi tạo dự án
```bash
ng new crud-app --routing --style=scss
cd crud-app
npm install
```

### Bước 2: Cấu hình environment
`src/environments/environment.ts`
```ts
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:5001/api'
};
```

### Bước 3: Model
`src/app/models/item.model.ts`
```ts
export interface Item {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
}
```

### Bước 4: Service gọi API
```bash
ng generate service services/item
```
`src/app/services/item.service.ts` (ý chính)
```ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Item } from '../models/item.model';
import { environment } from '../../environments/environment';

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

### Bước 5: Component danh sách (bảng) & form
- Tạo component standalone:
```bash
ng generate component components/item-list --standalone --flat --skip-tests
ng generate component components/item-form --standalone --flat --skip-tests
```

#### ItemListComponent (gợi ý)
- Input: `items: Item[]`
- Output: `edit(item)`, `remove(item)`, `view(item?)`
- Template: bảng như hình (SN, Product, Description, Created, Action); dùng `*ngFor`. Có phân trang đơn giản (client-side).

#### ItemFormComponent (gợi ý)
- Dùng Reactive Forms.
- Form control: `name` (required), `description`.
- Input: `item?: Item` (khi edit thì patchValue).
- Output: `save(formValue)`, `cancel()`.
- Nút submit đổi nhãn “Add Product” / “Update”.

### Bước 6: Trang container (ItemsPage)
- Tạo component container (standalone) để gắn List + Form.
- State: `items`, `selected`, `loading`, `showForm`.
- Luồng:
  - `ngOnInit()` → `load()` gọi `itemService.getAll()`.
  - Add: mở form create → `create(payload)` → prepend vào `items`.
  - Edit: set `selected` → form patch → `update(id, payload)` → replace trong `items`.
  - Delete: confirm → `delete(id)` → filter khỏi `items`.
  - Có thể refetch sau mỗi CRUD để đồng bộ (hoặc cập nhật local state).

Pseudo-code container:
```ts
load() {
  this.loading = true;
  this.svc.getAll().subscribe(r => { this.items = r; this.loading = false; });
}
onAdd(payload) {
  this.svc.create(payload).subscribe(newItem => this.items = [newItem, ...this.items]);
}
onEdit(item) { this.selected = item; this.showForm = true; }
onUpdate(id, payload) {
  this.svc.update(id, payload).subscribe(u => {
    this.items = this.items.map(x => x.id === id ? u : x);
    this.selected = null; this.showForm = false;
  });
}
onDelete(id) {
  this.svc.delete(id).subscribe(() => this.items = this.items.filter(x => x.id !== id));
}
```

### Bước 7: Routing
- `app.routes.ts`: route `/items` trỏ vào ItemsPage.
- `AppComponent` chỉ cần `<router-outlet></router-outlet>`.

### Bước 8: UI giống hình
- Cài Bootstrap:
```bash
npm install bootstrap
```
- `src/styles.scss`:
```scss
@import "bootstrap/dist/css/bootstrap.min.css";
```
- Table và nút:
  - Nút “Add Product” ở góc phải.
  - Hành động: View (optional), Edit (pencil), Delete (trash).
  - Phân trang: hiển thị 5 item/trang, nút chuyển trang.

### Bước 9: Chạy thử
- Backend:
```bash
cd CrudApi
dotnet run
```
- Frontend:
```bash
cd crud-app
ng serve
```
- Mở `http://localhost:4200/items` → Thực hiện Add/Edit/Delete, kiểm tra Network gọi API.

### Bước 10: Kiểm thử API trực tiếp
- Swagger: `https://localhost:5001/swagger`
- Thử:
  - POST /api/items
  - GET /api/items
  - PUT /api/items/{id}
  - DELETE /api/items/{id}

---

## Ghi chú nhanh
- Nếu muốn seed dữ liệu mẫu, tạo thêm `Data/Seed.cs` và gọi trong Program khi migrate.
- Nếu muốn disable auto-migrate trong production, bỏ đoạn `db.Database.Migrate();` và chạy `dotnet ef database update` thủ công trước khi deploy.
- FE có thể dùng Angular Material thay Bootstrap nếu quen; thay thế table + paginator tương ứng.

