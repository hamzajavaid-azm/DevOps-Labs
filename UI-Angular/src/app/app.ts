import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import { TaskService } from './task.service';
import { TaskItem } from './task.model';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly taskService = inject(TaskService);

  // --- UI state (signals) ---
  protected readonly tasks = signal<TaskItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly newTitle = signal('');
  protected readonly adding = signal(false);

  // Derived counters for the header.
  protected readonly remaining = computed(() => this.tasks().filter(t => !t.isDone).length);
  protected readonly total = computed(() => this.tasks().length);

  ngOnInit(): void {
    this.loadTasks();
  }

  // GET /api/tasks
  loadTasks(): void {
    this.loading.set(true);
    this.error.set(null);
    this.taskService.getTasks().subscribe({
      next: tasks => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(this.readError(err, 'Could not load tasks. Is the API running on http://localhost:5184?'));
        this.loading.set(false);
      }
    });
  }

  // POST /api/tasks
  addTask(): void {
    const title = this.newTitle().trim();
    if (!title || this.adding()) {
      return;
    }

    this.adding.set(true);
    this.error.set(null);
    this.taskService.createTask(title).subscribe({
      next: task => {
        this.tasks.update(list => [...list, task]);
        this.newTitle.set('');
        this.adding.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(this.readError(err, 'Could not add the task.'));
        this.adding.set(false);
      }
    });
  }

  // PUT /api/tasks/{id}/done
  markDone(task: TaskItem): void {
    if (task.isDone) {
      return;
    }

    this.error.set(null);
    this.taskService.markDone(task.id).subscribe({
      next: updated => {
        this.tasks.update(list => list.map(t => (t.id === updated.id ? updated : t)));
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(this.readError(err, 'Could not update the task.'));
      }
    });
  }

  // Pulls the API's validation message out of a 400 { errors: [...] } body,
  // otherwise falls back to a friendly default.
  private readError(err: HttpErrorResponse, fallback: string): string {
    const apiErrors = err.error?.errors;
    if (Array.isArray(apiErrors) && apiErrors.length > 0) {
      return apiErrors.join(' ');
    }
    return fallback;
  }
}
