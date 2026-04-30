import { useState } from 'react';
import { api } from '../../api/client';
import type { RawOidResult } from '../../types';

export default function OidScanTab() {
  const [baseOid, setBaseOid] = useState('1.3.6.1.4.1.935');
  const [withinSubtree, setWithinSubtree] = useState(true);
  const [rows, setRows] = useState<RawOidResult[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const startScan = async () => {
    setBusy(true);
    setError('');
    setRows([]);
    try {
      const result = await api.debug.walk(baseOid, withinSubtree);
      setRows(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Tarama başarısız');
    } finally {
      setBusy(false);
    }
  };

  const downloadTxt = () => {
    if (rows.length === 0) return;
    const lines = [
      'UpsPoC OID Tarama Sonucu',
      `Tarih: ${new Date().toISOString()}`,
      `Base OID: ${baseOid}`,
      '='.repeat(80),
      ...rows.map(r => `${r.oid} = ${r.value}`),
    ];
    const blob = new Blob([lines.join('\n')], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `oid-scan-${new Date().toISOString().replace(/[:.]/g, '-')}.txt`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-4">
      <div className="bg-slate-800 border border-slate-700 rounded-xl p-4">
        <p className="text-slate-400 text-xs mb-3">
          NetAgent özel OID ağacını tarar. Donanım versiyonu, seri numarası, kritik eşikler gibi
          özel alanların OID'sini bulmak için kullanılır.
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <label className="text-xs text-slate-400">Base OID:</label>
          <input
            type="text"
            value={baseOid}
            onChange={e => setBaseOid(e.target.value)}
            disabled={busy}
            className="flex-1 min-w-[200px] bg-slate-700 border border-slate-600 rounded px-2 py-1 text-xs text-slate-200 font-mono disabled:opacity-50 focus:outline-none focus:border-sky-500"
          />
          <label className="flex items-center gap-1 text-xs text-slate-400">
            <input
              type="checkbox"
              checked={withinSubtree}
              onChange={e => setWithinSubtree(e.target.checked)}
              disabled={busy}
            />
            Sadece alt ağaç
          </label>
          <button
            onClick={startScan}
            disabled={busy || !baseOid.trim()}
            className="bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-xs rounded px-3 py-1 transition-colors"
          >
            {busy ? 'Taranıyor...' : '▶ Taramayı Başlat'}
          </button>
          <button
            onClick={downloadTxt}
            disabled={rows.length === 0}
            className="bg-slate-700 hover:bg-slate-600 disabled:opacity-50 text-slate-200 text-xs rounded px-3 py-1 transition-colors"
          >
            ⬇ TXT İndir
          </button>
        </div>
        {error && <p className="text-red-400 text-xs mt-2">⚠ {error}</p>}
        {rows.length > 0 && (
          <p className="text-slate-400 text-xs mt-2">
            Bulunan OID: <span className="text-slate-200 font-medium">{rows.length}</span>
          </p>
        )}
      </div>

      {rows.length > 0 && (
        <div className="bg-slate-800 border border-slate-700 rounded-xl overflow-hidden">
          <div className="overflow-x-auto max-h-[60vh] overflow-y-auto">
            <table className="w-full text-xs">
              <thead className="sticky top-0 bg-slate-900">
                <tr className="text-slate-400">
                  <th className="text-left px-3 py-2 font-medium">OID</th>
                  <th className="text-left px-3 py-2 font-medium">Tip</th>
                  <th className="text-left px-3 py-2 font-medium">Değer</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r, i) => (
                  <tr key={i} className="border-t border-slate-700">
                    <td className="px-3 py-1 text-slate-300 font-mono text-[11px]">{r.oid}</td>
                    <td className="px-3 py-1 text-slate-500">{r.type}</td>
                    <td className="px-3 py-1 text-slate-200 font-mono text-[11px] break-all">{r.value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
