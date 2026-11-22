import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateTaskRequest, TaskItem } from './task.model';

// Base URL of the TaskTracker API (the default `dotnet run` / http profile).
// Change this if you run the API on a different host or port.
const API_BASE = 'http://localhost:5184/api/tasks';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  // GET /api/tasks — every task, oldest first.
  getTasks(): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(API_BASE);
  }

  // GET /api/tasks/{id} — a single task, or 404.
  getTask(id: number): Observable<TaskItem> {
    return this.http.get<TaskItem>(`${API_BASE}/${id}`);
  }

  // POST /api/tasks — create a task from its title; the API returns the saved task.
  createTask(title: string): Observable<TaskItem> {
    const body: CreateTaskRequest = { title };
    return this.http.post<TaskItem>(API_BASE, body);
  }

  // PUT /api/tasks/{id}/done — mark a task as done; the API returns the updated task.
  markDone(id: number): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${API_BASE}/${id}/done`, {});
  }
}
