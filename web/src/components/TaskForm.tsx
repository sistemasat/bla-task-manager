import { useEffect, useRef, useState, type FormEvent } from "react";
import { X } from "lucide-react";
import type { TaskInput, TaskItem, TaskStatus } from "../types";

export interface TaskFormProps {
  task?: TaskItem;
  onSave: (input: TaskInput, status: TaskStatus) => Promise<void>;
  onClose: () => void;
}
export function TaskForm({ task, onSave, onClose }: TaskFormProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleInput = useRef<HTMLInputElement>(null);
  const [title, setTitle] = useState(task?.title ?? "");
  const [description, setDescription] = useState(task?.description ?? "");
  const [dueDate, setDueDate] = useState(task?.due_date ?? "");
  const [status, setStatus] = useState<TaskStatus>(task?.status ?? "pending");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const element = dialog.current!;
    element.showModal();
    titleInput.current?.focus();
    return () => element.close();
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    if (!title.trim()) {
      setError("Enter a title for your task.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      await onSave(
        {
          title: title.trim(),
          description: description.trim() || null,
          due_date: dueDate || null,
        },
        status,
      );
      onClose();
    } catch (failure) {
      setError(
        failure instanceof Error
          ? failure.message
          : "Unable to save your task.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <dialog
      ref={dialog}
      className="task-dialog"
      aria-labelledby="task-form-title"
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) onClose();
      }}
    >
      <header className="dialog-header">
        <div>
          <span className="eyebrow">YOUR WORKSPACE</span>
          <h2 id="task-form-title">{task ? "Edit task" : "A new task"}</h2>
        </div>
        <button
          className="icon-button"
          aria-label="Close task form"
          disabled={busy}
          onClick={onClose}
        >
          <X size={20} />
        </button>
      </header>
      <form onSubmit={submit}>
        <label>
          Title
          <input
            ref={titleInput}
            required
            maxLength={200}
            placeholder="What needs to get done?"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
          />
        </label>
        <label>
          Description
          <textarea
            maxLength={4000}
            rows={4}
            placeholder="Add a little context…"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
        <div className="form-row">
          <label>
            Due date
            <input
              type="date"
              value={dueDate}
              onInput={(event) => setDueDate(event.currentTarget.value)}
              onChange={(event) => setDueDate(event.target.value)}
            />
          </label>
          {task && (
            <label>
              Status
              <select
                value={status}
                onChange={(event) =>
                  setStatus(event.target.value as TaskStatus)
                }
              >
                <option value="pending">Pending</option>
                <option value="in_progress">In progress</option>
                <option value="completed">Completed</option>
              </select>
            </label>
          )}
        </div>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <footer className="dialog-actions">
          <button
            type="button"
            className="button"
            disabled={busy}
            onClick={onClose}
          >
            Cancel
          </button>
          <button className="button primary" disabled={busy}>
            {busy ? "Saving…" : task ? "Save changes" : "Create task"}
          </button>
        </footer>
      </form>
    </dialog>
  );
}
