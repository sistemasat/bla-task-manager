import { useCallback, useEffect, useState } from 'react';
import { api } from './api';
import { AuthForm } from './components/AuthForm';
import { TasksPage } from './components/TasksPage';
import type { Credentials, Session } from './types';

export function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [expired, setExpired] = useState(false);
  const endSession = useCallback(() => { setSession(null); setExpired(true); }, []);

  useEffect(() => {
    if (!session) return;
    const timer = window.setTimeout(endSession, Math.max(0, Date.parse(session.access_token.expires_at) - Date.now()));
    return () => window.clearTimeout(timer);
  }, [session, endSession]);

  async function signIn(credentials: Credentials, registering: boolean) {
    if (registering) await api.register(credentials);
    setSession(await api.login(credentials));
    setExpired(false);
  }

  return session ? <TasksPage session={session} onSignOut={() => setSession(null)} onExpired={endSession} /> : <>
    {expired && <p className="session-notice" role="status">Your session has ended. Please sign in again.</p>}
    <AuthForm onSubmit={signIn} />
  </>;
}
