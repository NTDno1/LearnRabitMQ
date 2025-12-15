import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Item } from '../../models/item.model';

@Component({
  selector: 'app-item-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './item-form.component.html',
  styleUrls: ['./item-form.component.css']
})
export class ItemFormComponent implements OnChanges {
  @Input() item: Item | null = null;
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