import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ProductDto } from '../models/api.types';
import { Observable } from 'rxjs';

export interface CreateProductRequest {
  name: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
}

export interface UpdateProductRequest {
  name: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getCatalog(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>('/api/Products/GetCatalog');
  }

  getById(id: number): Observable<ProductDto> {
    return this.http.get<ProductDto>(`/api/Products/GetById/${id}`);
  }

  getMine(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>('/api/Products/GetMine');
  }

  create(body: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>('/api/Products/Create', body);
  }

  update(id: number, body: UpdateProductRequest): Observable<ProductDto> {
    return this.http.put<ProductDto>(`/api/Products/Update/${id}`, body);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/Products/Delete/${id}`);
  }
}
