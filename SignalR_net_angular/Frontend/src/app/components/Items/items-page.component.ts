import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Item } from '../../models/item.model';
import { ItemService } from '../../services/item.service';
import { ItemListComponent } from './item-list.component';
import { ItemFormComponent } from './item-form.component';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
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

  constructor(
    private itemService: ItemService, 
    private authService: AuthService,
    private router: Router) {}
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
  logout(): void {
    this.authService.logout();
    console.log('logout');
    this.router.navigate(['/auth']);
  }
  onCancel(): void { this.resetForm(); }
  private resetForm(): void { this.selected = null; this.showForm = false; }
}