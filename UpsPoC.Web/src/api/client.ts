import type {
  ConnectionRequest, UpsCommand, UpsConfig, UpsConnectionInfo,
  MetricDetail, DiagnosticResult, RawOidResult
} from '../types';

const BASE = '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const headers: HeadersInit = {};
  if (options?.body) {
    headers['Content-Type'] = 'application/json';
  }

  const res = await fetch(`${BASE}${url}`, {
    credentials: 'include',
    ...options,
    headers: {
      ...headers,
      ...options?.headers,
    },
  });

  if (res.status === 401) {
    window.dispatchEvent(new CustomEvent('auth:unauthorized'));
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
    getConnection: () => request<UpsConnectionInfo>('/ups/connection'),
    setConnection: (req: ConnectionRequest) =>
      request<UpsConnectionInfo>('/ups/connection', { method: 'POST', body: JSON.stringify(req) }),
    clearConnection: () => request<UpsConnectionInfo>('/ups/connection', { method: 'DELETE' }),
    getMetricsDetail: () => request<MetricDetail[]>('/ups/metrics-detail'),
    runDiagnostic: () => request<DiagnosticResult>('/ups/diagnostic', { method: 'POST' }),
    customSet: (oid: string, value: number) =>
      request<{ success: boolean }>('/ups/custom-set', { method: 'POST', body: JSON.stringify({ oid, value }) }),
    historyCsvUrl: '/api/ups/history.csv',
  },
  debug: {
    walk: (oid: string, withinSubtree = true) =>
      request<RawOidResult[]>(`/debug/walk?oid=${encodeURIComponent(oid)}&withinSubtree=${withinSubtree}`),
    get: (oid: string) =>
      request<RawOidResult>(`/debug/get?oid=${encodeURIComponent(oid)}`),
  },
};
