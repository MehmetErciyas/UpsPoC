import { useState, useEffect } from 'react';
import { api } from './api/client';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';

type AuthState = 'loading' | 'authenticated' | 'unauthenticated';

export default function App() {
  const [authState, setAuthState] = useState<AuthState>('loading');

  useEffect(() => {
    api.auth.me()
      .then(() => setAuthState('authenticated'))
      .catch(() => setAuthState('unauthenticated'));
  }, []);

  if (authState === 'loading') {
    return (
      <div className="min-h-screen bg-slate-900 flex items-center justify-center">
        <div className="text-slate-400 animate-pulse">Yükleniyor...</div>
      </div>
    );
  }

  if (authState === 'unauthenticated') {
    return <Login onLogin={() => setAuthState('authenticated')} />;
  }

  return <Dashboard onLogout={() => setAuthState('unauthenticated')} />;
}
