import type { Credentials, Session, TaskInput, TaskItem, TaskStatus } from './types';

export class ApiError extends Error {
  constructor(message: string, public readonly status: number) { super(message); }
}

export const api = {
  login: (_input: Credentials): Promise<Session> => { throw new Error('Not implemented'); },
  register: (_input: Credentials): Promise<void> => { throw new Error('Not implemented'); },
  listTasks: (_token: string, _skip: number, _signal?: AbortSignal): Promise<TaskItem[]> => { throw new Error('Not implemented'); },
  createTask: (_token: string, _input: TaskInput): Promise<TaskItem> => { throw new Error('Not implemented'); },
  updateTask: (_token: string, _id: string, _input: TaskInput, _status: TaskStatus): Promise<TaskItem> => { throw new Error('Not implemented'); },
  deleteTask: (_token: string, _id: string): Promise<void> => { throw new Error('Not implemented'); },
};
