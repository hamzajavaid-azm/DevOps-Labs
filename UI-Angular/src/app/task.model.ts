// Mirrors the TaskItem entity returned by the Todo API (API/Lab-01/Data/TaskItem.cs).
export interface TaskItem {
  id: number;
  title: string;
  isDone: boolean;
  createdAt: string; // ISO-8601 UTC timestamp
}

// Request body for POST /api/tasks.
export interface CreateTaskRequest {
  title: string;
}
