import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

interface TodoItem {
  id: number;
  title: string;
  isCompleted: boolean;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly http = inject(HttpClient);

  protected readonly title = 'frontend (standalone)';
  protected readonly todoItems = signal<TodoItem[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal('');

  constructor() {
    this.loadTodoItems();
  }

  protected loadTodoItems(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.http.get<TodoItem[]>('/api/TodoItems').subscribe({
      next: (items) => {
        this.todoItems.set(items);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 0) {
          this.errorMessage.set('Cannot connect to backend API.');
        } else {
          this.errorMessage.set(`Backend API error: HTTP ${error.status}`);
        }
        this.isLoading.set(false);
      },
    });
  }
}
