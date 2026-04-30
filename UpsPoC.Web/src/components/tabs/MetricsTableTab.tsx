import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { MetricDetail } from '../../types';

interface Props {
  refreshKey: number; // her artışında yeniden fetch
}

export default function MetricsTableTab({ refreshKey }: Props) {
  const [rows, setRows] = useState<MetricDetail[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const reload = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.ups.getMetricsDetail();
      setRows(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { reload(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [refreshKey]);

  const groups: { label: string; key: 'live' | 'info' | 'f-equivalent' }[] = [
    { label: 'Canlı UPS Değerleri', key: 'live' },
    { label: 'Sistem Bilgileri', key: 'info' },
    { label: 'SNMP F Karşılığı (Nominal)', key: 'f-equivalent' },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-slate-400 text-xs">
          Python tool'undaki <em>Canlı/Sistem/F Karşılığı</em> sekmelerinin birleşik tablo görünümü.
        </p>
        <button
          onClick={reload}
          disabled={loading}
          className="bg-slate-700 hover:bg-slate-600 disabled:opacity-50 text-slate-200 text-xs rounded px-3 py-1 transition-colors"
        >
          {loading ? 'Yükleniyor...' : '↻ Yenile'}
        </button>
      </div>

      {error && <p className="text-red-400 text-xs">⚠ {error}</p>}

      {groups.map(g => {
        const groupRows = rows.filter(r => r.group === g.key);
        if (groupRows.length === 0 && !loading) return null;
        return (
          <div key={g.key} className="bg-slate-800 border border-slate-700 rounded-xl overflow-hidden">
            <div className="bg-slate-900/50 px-4 py-2 border-b border-slate-700">
              <h3 className="text-slate-200 text-sm font-semibold">{g.label}</h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="bg-slate-900/30 text-slate-400">
                    <th className="text-left px-3 py-2 font-medium">Parametre</th>
                    <th className="text-left px-3 py-2 font-medium">Değer</th>
                    <th className="text-left px-3 py-2 font-medium">Ham</th>
                    <th className="text-left px-3 py-2 font-medium">OID</th>
                    <th className="text-center px-3 py-2 font-medium">Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {groupRows.map(r => (
                    <tr key={r.key} className={`border-t border-slate-700 ${r.ok ? '' : 'bg-red-950/20'}`}>
                      <td className="px-3 py-1.5 text-slate-300">{r.title}</td>
                      <td className="px-3 py-1.5 text-slate-200 font-medium">{r.valueText || '—'}</td>
                      <td className="px-3 py-1.5 text-slate-500 font-mono">{r.rawValue || '—'}</td>
                      <td className="px-3 py-1.5 text-slate-500 font-mono text-[11px]">{r.oid}</td>
                      <td className="px-3 py-1.5 text-center">
                        {r.ok
                          ? <span className="text-emerald-400">OK</span>
                          : <span className="text-red-400" title={r.error}>HATA</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        );
      })}
    </div>
  );
}
