import { useState, type FormEvent } from 'react';
import { ArrowRight, Check } from 'lucide-react';
import type { Credentials } from '../types';

export interface AuthFormProps {
  onSubmit: (credentials: Credentials, registering: boolean) => Promise<void>;
}
export function AuthForm({ onSubmit }: AuthFormProps) {
  const [registering, setRegistering] = useState(false);
  const [credentials, setCredentials] = useState<Credentials>({ name: '', email: '', password: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    setBusy(true); setError('');
    try { await onSubmit(credentials, registering); }
    catch (failure) { setError(failure instanceof Error ? failure.message : 'Unable to sign in.'); }
    finally { setBusy(false); }
  }

  function change(field: keyof Credentials, value: string) {
    setCredentials(current => ({ ...current, [field]: value }));
  }

  return <main className="auth-layout">
    <section className="auth-story">
      <div className="brand"><span className="brand-mark"><Check size={21} /></span>Task Manager</div>
      <div className="auth-copy"><span className="eyebrow">A LITTLE MORE CLARITY</span>
        <h1>Make room for<br />what matters.</h1>
        <p>A simple place for your tasks, deadlines and the next thing you want to get done.</p>
        <div className="preview-card"><span className="preview-check"><Check size={16} /></span><div><strong>One thing at a time.</strong><small>Less clutter. More progress.</small></div></div>
      </div>
      <small className="auth-footnote">Your tasks. Your space.</small>
    </section>
    <section className="auth-panel"><div className="auth-form-wrap">
      <span className="eyebrow">LET'S GET STARTED</span>
      <h2>{registering ? 'Create your account' : 'Welcome back'}</h2>
      <p>{registering ? 'A fresh start for your everyday work.' : 'Sign in to pick up where you left off.'}</p>
      <form onSubmit={submit}>
        {registering && <label>Name<input required maxLength={100} autoComplete="name" value={credentials.name} onChange={e => change('name', e.target.value)} /></label>}
        <label>Email<input required type="email" maxLength={254} autoComplete="email" value={credentials.email} onChange={e => change('email', e.target.value)} /></label>
        <label>Password<input required type="password" minLength={registering ? 12 : undefined} maxLength={128} autoComplete={registering ? 'new-password' : 'current-password'} value={credentials.password} onChange={e => change('password', e.target.value)} /></label>
        {registering && <small className="field-hint">Use at least 12 characters.</small>}
        {error && <p className="error" role="alert">{error}</p>}
        <button className="button primary wide" disabled={busy}>{busy ? 'Please wait…' : registering ? 'Create account' : 'Sign in'}<ArrowRight size={17} /></button>
      </form>
      <p className="auth-switch">{registering ? 'Already have an account?' : 'New here?'} <button className="text-button" disabled={busy} onClick={() => { setRegistering(!registering); setError(''); }}>{registering ? 'Sign in' : 'Create account'}</button></p>
      {!registering && <div className="demo-box"><span>Just taking a look?</span><button className="text-button" disabled={busy} onClick={() => setCredentials({ name: '', email: 'demo@example.com', password: 'DemoPassword123!' })}>Use demo account</button><small>demo@example.com · DemoPassword123!</small></div>}
    </div></section>
  </main>;
}
