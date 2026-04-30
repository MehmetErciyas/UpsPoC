import { useState } from 'react';
import { api } from '../../api/client';
import CommandPanel from '../CommandPanel';
import type { DiagnosticResult } from '../../types';

export default function ControlTab() {
  const [diag, setDiag] = useState<DiagnosticResult | null>(null);
  const [diagBusy, setDiagBusy] = useState(false);
  const [diagError, setDiagError] = useState('');

  const [customOid, setCustomOid] = useState('1.3.6.1.4.1.935.1.1.1.6.2.3.0');
  const [customValue, setCustomValue] = useState(2);
  const [confirmText, setConfirmText] = useState('');
  const [riskAccepted, setRiskAccepted] = useState(false);
  const [customResult, setCustomResult] = useState<string>('');
  const [customBusy, setCustomBusy] = useState(false);

  const runDiagnostic = async () => {
    setDiagBusy(true);
    setDiagError('');
    setDiag(null);
    try {
      const result = await api.ups.runDiagnostic();
      setDiag(result);
    } catch (err) {
      setDiagError(err instanceof Error ? err.message : 'Test başarısız');
    } finally {
      setDiagBusy(false);
    }
  };

  const sendCustom = async () => {
    if (confirmText.trim().toUpperCase() !== 'KAPAT') {
      setCustomResult('⚠ Onay kutusuna KAPAT yazmalısınız.');
      return;
    }
    if (!riskAccepted) {
      setCustomResult('⚠ Riski kabul ettiğinizi işaretlemeden komut gönderilemez.');
      return;
    }
    if (!confirm(`Özel OID SET gönderilecek:\n${customOid} = ${customValue}\n\nDevam edilsin mi?`)) return;

    setCustomBusy(true);
    setCustomResult('');
    try {
      await api.ups.customSet(customOid, customValue);
      setCustomResult(`✓ SET başarılı: ${customOid} = ${customValue}`);
    } catch (err) {
      setCustomResult(`✗ HATA: ${err instanceof Error ? err.message : 'gönderilemedi'}`);
    } finally {
      setCustomBusy(false);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <CommandPanel />

      {/* SET yetki testi */}
      <div className="bg-slate-800 border border-slate-700 rounded-xl p-4">
        <h3 className="text-slate-200 text-sm font-semibold mb-2">SET Yetki Testi</h3>
        <p className="text-slate-500 text-xs mb-3">
          Read/write community ile sysName ve shutdown OID erişimini kontrol eder. SNMP SET göndermez.
        </p>
        <button
          onClick={runDiagnostic}
          disabled={diagBusy}
          className="bg-sky-600 hover:bg-sky-500 disabled:opacity-50 text-white text-xs rounded px-3 py-1.5 transition-colors mb-3"
        >
          {diagBusy ? 'Test ediliyor...' : '▶ Testi Başlat'}
        </button>

        {diagError && <p className="text-red-400 text-xs mb-2">⚠ {diagError}</p>}

        {diag && (
          <div className="space-y-2">
            {diag.lines.map((line, i) => (
              <div key={i} className={`text-xs p-2 rounded border ${
                line.ok ? 'bg-emerald-950/30 border-emerald-800 text-emerald-300'
                        : 'bg-red-950/30 border-red-800 text-red-300'
              }`}>
                <div className="flex justify-between gap-2">
                  <span className="font-medium">{line.ok ? 'OK' : 'HATA'} | {line.title}</span>
                </div>
                <div className="text-[11px] text-slate-400 font-mono mt-0.5">OID: {line.oid}</div>
                {line.ok ? (
                  <div className="text-[11px] mt-0.5">Değer: {line.value}</div>
                ) : (
                  <div className="text-[11px] mt-0.5">Hata: {line.error}</div>
                )}
              </div>
            ))}
            {diag.hints.length > 0 && (
              <div className="pt-2 mt-2 border-t border-slate-700">
                <p className="text-slate-400 text-[11px] uppercase tracking-wider mb-1">Yorum</p>
                <ul className="space-y-1">
                  {diag.hints.map((h, i) => (
                    <li key={i} className="text-slate-400 text-xs">• {h}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Özel OID SET */}
      <div className="bg-slate-800 border border-slate-700 rounded-xl p-4 lg:col-span-2">
        <h3 className="text-slate-200 text-sm font-semibold mb-2">Özel OID SET (Tehlikeli)</h3>
        <p className="text-red-400 text-xs mb-3">
          ⚠ Yanlış kullanım UPS çıkışını kapatabilir. Sadece test ortamında ve write community doğru tanımlandıysa kullanın.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-3">
          <div>
            <label className="block text-xs text-slate-400 mb-1">OID</label>
            <input
              type="text"
              value={customOid}
              onChange={e => setCustomOid(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded px-3 py-1.5 text-sm text-slate-200 font-mono focus:outline-none focus:border-sky-500"
            />
          </div>
          <div>
            <label className="block text-xs text-slate-400 mb-1">Değer (Integer32)</label>
            <input
              type="number"
              value={customValue}
              onChange={e => setCustomValue(Number(e.target.value))}
              className="w-full bg-slate-700 border border-slate-600 rounded px-3 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-sky-500"
            />
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3 mb-3">
          <div className="flex items-center gap-2">
            <label className="text-xs text-slate-400">Onay (KAPAT yazın):</label>
            <input
              type="text"
              value={confirmText}
              onChange={e => setConfirmText(e.target.value)}
              className="w-24 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-xs text-slate-200 focus:outline-none focus:border-sky-500"
            />
          </div>
          <label className="flex items-center gap-2 text-xs text-slate-400">
            <input
              type="checkbox"
              checked={riskAccepted}
              onChange={e => setRiskAccepted(e.target.checked)}
            />
            Riski kabul ediyorum.
          </label>
          <button
            onClick={sendCustom}
            disabled={customBusy}
            className="ml-auto bg-red-700 hover:bg-red-600 disabled:opacity-50 text-white text-xs rounded px-4 py-1.5 transition-colors"
          >
            {customBusy ? 'Gönderiliyor...' : '⬛ Özel OID Gönder'}
          </button>
        </div>

        {customResult && (
          <p className={`text-xs ${customResult.startsWith('✓') ? 'text-emerald-400' : 'text-red-400'}`}>
            {customResult}
          </p>
        )}
      </div>
    </div>
  );
}
