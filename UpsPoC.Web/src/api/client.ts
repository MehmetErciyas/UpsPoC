import type { UpsCommand, UpsConfig } from '../types';

const BASE = '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (res.status === 401) {
    window.location.href = '/';
    throw new Error('Oturum süresi doldu');
  }

  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(body.error ?? res.statusText);
  }

  return res.json();
}

export const api = {
  auth: {
    login: (username: string, password: string) =>
      request<{ username: string }>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
      }),
    logout: () => request<{ success: boolean }>('/auth/logout', { method: 'POST' }),
    me: () => request<{ username: string }>('/auth/me'),
  },
  ups: {
    getStatus: () => request<import('../types').UpsStatus>('/ups/status'),
    getHistory: () => request<import('../types').UpsSnapshot[]>('/ups/history'),
    getConfig: () => request<UpsConfig>('/ups/config'),
    sendCommand: (cmd: UpsCommand) =>
      request<{ success: boolean }>('/ups/command', { method: 'POST', body: JSON.stringify(cmd) }),
    setConfig: (config: UpsConfig) =>
      request<{ success: boolean }>('/ups/config', { method: 'POST', body: JSON.stringify(config) }),
  },
};
