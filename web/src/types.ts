export type TaskStatus = 'pending' | 'in_progress' | 'completed';
export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  due_date: string | null;
  created_at: string;
  updated_at: string;
}
export interface TaskInput { title: string; description: string | null; due_date: string | null }
export interface Credentials { name: string; email: string; password: string }
export interface Session {
  access_token: { token: string; expires_at: string };
  user: { id: string; name: string; email: string };
}
