import { useState, useEffect, useRef } from 'react';
import { api } from '../api/client';
import type { UpsConfig } from '../types';

interface Props {
  onClose: () => void;
}

export default function ConfigModal({ onClose }: Props) {
  const [config, setConfig] = useState<UpsConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    };
  }, []);

  useEffect(() => {
    api.ups.getConfig()
      .then(setConfig)
      .catch(err => setError(err instanceof Error ? err.message : 'Yüklenemedi'))
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    if (!config) return;
    setSaving(true);
    setError('');
    try {
      await api.ups.setConfig(config);
      setSuccess(true);
      saveTimerRef.current = setTimeout(() => { setSuccess(false); onClose(); }, 1200);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Kaydedilemedi');
    } finally {
      setSaving(false);
    }
  };

  const update = (key: keyof UpsConfig, val: number) =>
    setConfig(prev => prev ? { ...prev, [key]: val } : prev);

  const fields: [string, keyof UpsConfig][] = [
    ['Nominal Giriş Voltajı (V)', 'inputVoltageNominal'],
    ['Nominal Giriş Frekansı (0.1Hz, örn. 500=50Hz)', 'inputFreqNominal'],
    ['Nominal Çıkış Voltajı (V)', 'outputVoltageNominal'],
    ['Nominal Çıkış Frekansı (0.1Hz)', 'outputFreqNominal'],
    ['Düşük Batarya Eşiği (dakika)', 'lowBatteryMinutes'],
    ['Düşük Voltaj Transfer Noktası (V)', 'lowVoltageTransferPoint'],
    ['Yüksek Voltaj Transfer Noktası (V)', 'highVoltageTransferPoint'],
  ];

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-slate-800 border border-slate-600 rounded-xl p-6 max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-slate-100 font-semibold">UPS Konfigürasyonu</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-200 text-xl leading-none">✕</button>
        </div>

        {loading && <p className="text-slate-400 text-sm">Yükleniyor...</p>}
        {error && <p className="text-red-400 text-sm bg-red-900/20 border border-red-800 rounded p-2 mb-3">{error}</p>}
        {success && <p className="text-emerald-400 text-sm mb-3">✓ Kaydedildi</p>}

        {config && (
          <div className="space-y-3">
            {fields.map(([label, key]) => (
              <div key={key}>
                <label className="block text-xs text-slate-400 mb-1">{label}</label>
                <input
                  type="number"
                  value={config[key] as number}
                  onChange={e => update(key, Number(e.target.value))}
                  className="w-full bg-slate-700 border border-slate-600 rounded px-3 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-sky-500"
                />
              </div>
            ))}

            <div>
              <label className="block text-xs text-slate-400 mb-1">Sesli Alarm</label>
              <select
                value={config.audibleStatus}
                onChange={e => update('audibleStatus', Number(e.target.value))}
                className="w-full bg-slate-700 border border-slate-600 rounded px-3 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-sky-500"
              >
                <option value={1}>Kapalı</option>
                <option value={2}>Açık</option>
                <option value={3}>Geçici Sessiz</option>
              </select>
            </div>

            <div className="flex gap-3 justify-end pt-2">
              <button onClick={onClose} className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded-lg text-sm transition-colors">
                İptal
              </button>
              <button
                onClick={handleSave}
                disabled={saving}
                className="px-4 py-2 bg-sky-600 hover:bg-sky-500 disabled:opacity-50 rounded-lg text-sm font-medium transition-colors"
              >
                {saving ? 'Kaydediliyor...' : 'Kaydet'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
