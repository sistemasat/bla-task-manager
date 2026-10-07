import type { Credentials } from '../types';

export interface AuthFormProps {
  onSubmit: (credentials: Credentials, registering: boolean) => Promise<void>;
}
export function AuthForm(_props: AuthFormProps) { return <div />; }
