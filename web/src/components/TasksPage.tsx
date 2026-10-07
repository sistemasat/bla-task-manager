import { useCallback, useEffect, useState } from 'react';
import { Check, ChevronLeft, ChevronRight, ClipboardList, LogOut, Pencil, Plus, RotateCw, Trash2, CalendarDays } from 'lucide-react';
import { api, ApiError } from '../api';
import type { Session, TaskInput, TaskItem, TaskStatus } from '../types';
import { TaskForm } from './TaskForm';

const statusLabels: Record<TaskStatus, string> = { pending: 'Pending', in_progress: 'In progress', completed: 'Completed' };

export function TasksPage({ session, onSignOut, onExpired }: { session: Session; onSignOut: () => void; onExpired: () => void }) {
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [page, setPage] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [editing, setEditing] = useState<TaskItem | 'new' | null>(null);
  const [deleting, setDeleting] = useState<TaskItem | null>(null);
  const [busy, setBusy] = useState(false);
  const [revision, setRevision] = useState(0);
  const token = session.access_token.token;

  const report = useCallback((failure: unknown) => {
    if (failure instanceof ApiError && failure.status === 401) { onExpired(); return; }
    setError(failure instanceof Error ? failure.message : 'Unable to load your tasks.');
  }, [onExpired]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError('');
    api.listTasks(token, page * 50, controller.signal)
      .then(items => { if (!controller.signal.aborted) setTasks(items); })
      .catch(failure => { if (!controller.signal.aborted) report(failure); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [token, page, revision, report]);

  async function save(input: TaskInput, status: TaskStatus) {
    try {
      if (editing && editing !== 'new') await api.updateTask(token, editing.id, input, status);
      else await api.createTask(token, input);
      setNotice(editing === 'new' ? 'Task created.' : 'Changes saved.');
      setPage(0); setRevision(value => value + 1);
    } catch (failure) {
      if (failure instanceof ApiError && failure.status === 401) onExpired();
      throw failure;
    }
  }

  async function remove() {
    if (!deleting || busy) return;
    setBusy(true); setError('');
    try {
      await api.deleteTask(token, deleting.id);
      setDeleting(null); setNotice('Task deleted.');
      if (tasks.length === 1 && page > 0) setPage(value => value - 1);
      else setRevision(value => value + 1);
    } catch (failure) { report(failure); setDeleting(null); }
    finally { setBusy(false); }
  }

  const visible = tasks.slice(0, 50);
  const today = localDate();

  return <div className="workspace">
    <aside className="sidebar"><div className="brand"><span className="brand-mark"><Check size={20} /></span>Task Manager</div>
      <div className="sidebar-label">WORKSPACE</div><div className="nav-item"><ClipboardList size={18} />My tasks</div>
      <div className="sidebar-note"><span className="note-dot" /><strong>A little progress, every day.</strong><p>Keep your next steps close.</p></div>
      <div className="profile"><span className="avatar">{session.user.name.slice(0, 1).toUpperCase()}</span><div><strong>{session.user.name}</strong><small>{session.user.email}</small></div></div>
      <button className="sign-out" onClick={onSignOut}><LogOut size={16} />Sign out</button>
    </aside>
    <main className="main-content">
      <header className="top-bar"><span>My workspace <span className="breadcrumb-divider">/</span> Tasks</span><span className="today"><CalendarDays size={15} />{new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date())}</span><button className="mobile-signout icon-button" aria-label="Sign out" onClick={onSignOut}><LogOut size={18} /></button></header>
      <div className="page-content"><div className="page-heading"><div><span className="eyebrow">ONE STEP AT A TIME</span><h1>My tasks</h1><p>A clear view of what comes next.</p></div><button className="button primary" onClick={() => setEditing('new')}><Plus size={17} />New task</button></div>
        <div className="list-toolbar"><span><span className="list-dot" />All tasks</span><button className="icon-button" aria-label="Refresh tasks" disabled={loading} onClick={() => setRevision(value => value + 1)}><RotateCw size={16} /></button></div>
        {error && <div className="error" role="alert">{error}<button className="text-button" onClick={() => setRevision(value => value + 1)}>Try again</button></div>}
        {notice && <p className="notice" role="status">{notice}</p>}
        {loading ? <div className="empty-state" role="status">Loading your tasks…</div> : !error && visible.length === 0 ? <div className="empty-state"><span className="empty-icon"><ClipboardList size={29} /></span><h2>A little room to begin</h2><p>Add your first task and take it from there.</p><button className="button" onClick={() => setEditing('new')}><Plus size={16} />Create your first task</button></div> : !error && <div className="task-list">
          {visible.map(task => <article key={task.id} className={`task-card ${task.status === 'completed' ? 'is-completed' : ''}`}>
            <span className={`task-indicator ${task.status}`} aria-hidden="true">{task.status === 'completed' && <Check size={15} />}</span>
            <div className="task-content"><button className="task-title" onClick={() => setEditing(task)}>{task.title}</button>{task.description && <p>{task.description}</p>}<div className="task-meta"><span className={`status-pill ${task.status}`}>{statusLabels[task.status]}</span>{task.due_date && <span className={`due-date ${task.due_date < today && task.status !== 'completed' ? 'overdue' : ''}`}><CalendarDays size={13} />{formatDueDate(task.due_date, today)}</span>}</div></div>
            <div className="task-actions"><button className="icon-button" aria-label={`Edit ${task.title}`} onClick={() => setEditing(task)}><Pencil size={16} /></button><button className="icon-button delete-button" aria-label={`Delete ${task.title}`} onClick={() => setDeleting(task)}><Trash2 size={16} /></button></div>
          </article>)}
        </div>}
        {!loading && !error && <footer className="list-footer"><span>{visible.length} {visible.length === 1 ? 'task' : 'tasks'} · Page {page + 1}</span><div><button className="icon-button" aria-label="Previous page" disabled={page === 0} onClick={() => setPage(value => value - 1)}><ChevronLeft size={17} /></button><button className="icon-button" aria-label="Next page" disabled={tasks.length <= 50} onClick={() => setPage(value => value + 1)}><ChevronRight size={17} /></button></div></footer>}
      </div>
    </main>
    {editing && <TaskForm task={editing === 'new' ? undefined : editing} onSave={save} onClose={() => setEditing(null)} />}
    {deleting && <DeleteDialog task={deleting} busy={busy} onCancel={() => setDeleting(null)} onConfirm={remove} />}
  </div>;
}

function DeleteDialog({ task, busy, onCancel, onConfirm }: { task: TaskItem; busy: boolean; onCancel: () => void; onConfirm: () => void }) {
  const [element, setElement] = useState<HTMLDialogElement | null>(null);
  useEffect(() => { element?.showModal(); return () => element?.close(); }, [element]);
  return <dialog ref={setElement} className="task-dialog delete-dialog" aria-labelledby="delete-title" onCancel={event => { event.preventDefault(); if (!busy) onCancel(); }}><h2 id="delete-title">Delete this task?</h2><p>“{task.title}” will be permanently deleted.</p><div className="dialog-actions"><button className="button" disabled={busy} onClick={onCancel}>Keep task</button><button className="button danger" disabled={busy} onClick={onConfirm}>{busy ? 'Deleting…' : 'Delete task'}</button></div></dialog>;
}

function localDate() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

function formatDueDate(value: string, today: string) {
  return value === today ? 'Today' : new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(`${value}T12:00:00`));
}
