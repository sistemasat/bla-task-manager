import type { TaskInput, TaskItem, TaskStatus } from '../types';

export interface TaskFormProps {
  task?: TaskItem;
  onSave: (input: TaskInput, status: TaskStatus) => Promise<void>;
  onClose: () => void;
}
export function TaskForm(_props: TaskFormProps) { return <div />; }
