import { useState, useEffect, useCallback, useRef } from 'react';
import { api } from '../api/client';
import type { UpsStatus, UpsSnapshot } from '../types';

export function useUpsData(intervalSeconds: number) {
  const [status, setStatus] = useState<UpsStatus | null>(null);
  const [history, setHistory] = useState<UpsSnapshot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchData = useCallback(async () => {
    try {
      const [newStatus, newHistory] = await Promise.all([
        api.ups.getStatus(),
        api.ups.getHistory(),
      ]);
      setStatus(newStatus);
      setHistory(newHistory);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bağlantı hatası');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
    intervalRef.current = setInterval(fetchData, intervalSeconds * 1000);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [fetchData, intervalSeconds]);

  return { status, history, isLoading, error, refetch: fetchData };
}
