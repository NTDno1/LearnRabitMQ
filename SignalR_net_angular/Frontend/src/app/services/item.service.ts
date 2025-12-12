import { Injectable } from "@angular/core";
import { environment } from "../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { Item } from "../models/item.model";

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