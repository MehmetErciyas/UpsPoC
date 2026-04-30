import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { UpsConnectionInfo } from '../types';

interface Props {
  connection: UpsConnectionInfo | null;
  onChange: (info: UpsConnectionInfo) => void;
}

export default function ConnectionBar({ connection, onChange }: Props) {
  const [host, setHost] = useState('');
  const [port, setPort] = useState(161);
  const [readCommunity, setReadCommunity] = useState('public');
  const [writeCommunity, setWriteCommunity] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  // Mevcut bağlantı bilgisi geldiğinde form'u doldur (sadece başlangıçta).
  useEffect(() => {
    if (connection) {
      setHost(connection.host);
      setPort(connection.port);
      setReadCommunity(connection.readCommunity);
      // hasWriteCommunity true olsa bile değer dönmüyor; boş kalsın, kullanıcı yeniden girsin.
    }
  }, [connection?.host, connection?.port, connection?.readCommunity]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleConnect = async () => {
    setError('');
    setBusy(true);
    try {
      const result = await api.ups.setConnection({
        host: host.trim(),
        port,
        readCommunity: readCommunity.trim(),
        writeCommunity: writeCommunity.trim() || undefined,
      });
      onChange(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bağlanılamadı');
    } finally {
      setBusy(false);
    }
  };

  const handleDisconnect = async () => {
    setBusy(true);
    try {
      const result = await api.ups.clearConnection();
      onChange(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Durdurulamadı');
    } finally {
      setBusy(false);
    }
  };

  const isConnected = connection?.isConfigured ?? false;

  return (
    <div className="bg-slate-800 border border-slate-700 rounded-xl p-3 mb-4">
      <div className="flex flex-wrap items-center gap-2 text-xs">
        <span className="text-slate-400 uppercase tracking-wider mr-2">Bağlantı</span>

        <div className="flex items-center gap-1">
          <label className="text-slate-500">IP:</label>
          <input
            type="text"
            value={host}
            onChange={e => setHost(e.target.value)}
            disabled={isConnected || busy}
            placeholder="192.168.1.50"
            className="w-32 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-slate-200 disabled:opacity-50 focus:outline-none focus:border-sky-500"
          />
        </div>

        <div className="flex items-center gap-1">
          <label className="text-slate-500">Port:</label>
          <input
            type="number"
            value={port}
            onChange={e => setPort(Number(e.target.value))}
            disabled={isConnected || busy}
            className="w-16 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-slate-200 disabled:opacity-50 focus:outline-none focus:border-sky-500"
            min={1}
            max={65535}
          />
        </div>

        <div className="flex items-center gap-1">
          <label className="text-slate-500">Read:</label>
          <input
            type="text"
            value={readCommunity}
            onChange={e => setReadCommunity(e.target.value)}
            disabled={isConnected || busy}
            placeholder="public"
            className="w-24 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-slate-200 disabled:opacity-50 focus:outline-none focus:border-sky-500"
          />
        </div>

        <div className="flex items-center gap-1">
          <label className="text-slate-500">Write:</label>
          <input
            type="password"
            value={writeCommunity}
            onChange={e => setWriteCommunity(e.target.value)}
            disabled={isConnected || busy}
            placeholder="private"
            className="w-24 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-slate-200 disabled:opacity-50 focus:outline-none focus:border-sky-500"
          />
        </div>

        {!isConnected ? (
          <button
            onClick={handleConnect}
            disabled={busy || !host.trim() || !readCommunity.trim()}
            className="ml-auto bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed rounded px-4 py-1 text-white font-medium transition-colors"
          >
            {busy ? 'Bağlanıyor...' : '▶ Bağlan'}
          </button>
        ) : (
          <button
            onClick={handleDisconnect}
            disabled={busy}
            className="ml-auto bg-red-700 hover:bg-red-600 disabled:opacity-50 rounded px-4 py-1 text-white font-medium transition-colors"
          >
            {busy ? 'Durduruluyor...' : '■ Durdur'}
          </button>
        )}
      </div>

      {error && (
        <p className="text-red-400 text-xs mt-2">⚠ {error}</p>
      )}
    </div>
  );
}
