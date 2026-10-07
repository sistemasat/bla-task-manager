import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { AuthForm } from './AuthForm';
import { TaskForm } from './TaskForm';

describe('Authentication form', () => {
  it('submits sign-in credentials', async () => {
    const submit = vi.fn().mockResolvedValue(undefined);
    render(<AuthForm onSubmit={submit} />);
    await userEvent.type(screen.getByLabelText('Email'), 'alex@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'StrongPassword!');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in', exact: true }));
    expect(submit).toHaveBeenCalledWith({ name: '', email: 'alex@example.com', password: 'StrongPassword!' }, false);
  });
  it('shows registration fields and server errors', async () => {
    const submit = vi.fn().mockRejectedValue(new Error('An account with this email already exists.'));
    render(<AuthForm onSubmit={submit} />);
    await userEvent.click(screen.getByRole('button', { name: 'Create account', exact: true }));
    await userEvent.type(screen.getByLabelText('Name'), 'Alex');
    await userEvent.type(screen.getByLabelText('Email'), 'alex@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'StrongPassword!');
    await userEvent.click(screen.getByRole('button', { name: 'Create account', exact: true }));
    expect(await screen.findByRole('alert')).toHaveTextContent('already exists');
  });
});

describe('Task form', () => {
  it('rejects whitespace-only title before calling the API', async () => {
    const save = vi.fn();
    render(<TaskForm onSave={save} onClose={vi.fn()} />);
    await userEvent.type(screen.getByLabelText('Title'), '   ');
    await userEvent.click(screen.getByRole('button', { name: 'Create task' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Enter a title');
    expect(save).not.toHaveBeenCalled();
  });
  it('submits a date-only value and disables duplicate submission', async () => {
    let finish!: () => void;
    const save = vi.fn().mockImplementation(() => new Promise<void>(resolve => { finish = resolve; }));
    const close = vi.fn();
    render(<TaskForm onSave={save} onClose={close} />);
    await userEvent.type(screen.getByLabelText('Title'), 'Prepare demo');
    await userEvent.type(screen.getByLabelText('Due date'), '2026-10-09');
    await userEvent.click(screen.getByRole('button', { name: 'Create task' }));
    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
    expect(save).toHaveBeenCalledWith({ title: 'Prepare demo', description: null, due_date: '2026-10-09' }, 'pending');
    finish();
    await waitFor(() => expect(close).toHaveBeenCalled());
  });
  it('keeps the form open when saving fails', async () => {
    const close = vi.fn();
    render(<TaskForm onSave={vi.fn().mockRejectedValue(new Error('Unable to save.'))} onClose={close} />);
    await userEvent.type(screen.getByLabelText('Title'), 'Task');
    await userEvent.click(screen.getByRole('button', { name: 'Create task' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to save');
    expect(close).not.toHaveBeenCalled();
  });
});
