import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Item } from '../../models/item.model';

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
  @Output() logout = new EventEmitter<void>();
} 