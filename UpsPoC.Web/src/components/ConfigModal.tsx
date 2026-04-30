import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { UpsConfig } from '../types';

interface Props {
  onClose: () => void;
}

export default function ConfigModal({ onClose }: Props) {
  const [config, setConfig] = useState<UpsConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api.ups.getConfig()
      .then(setConfig)
      .catch(err => setError(err instanceof Error ? err.message : 'Yüklenemedi'))
      .finally(() => setLoading(false));
  }, []);

  const rows: [string, string][] = config ? [
    ['Nominal Çıkış Voltajı', `${(config.nominalOutputVoltage ?? 0).toFixed(1)} V`],
    ['Nominal Batarya Voltajı', `${(config.nominalBatteryVoltage ?? 0).toFixed(1)} V`],
    ['Kritik Yük Eşiği', `${config.criticalLoadPercent ?? 0} %`],
    ['Kritik Sıcaklık Eşiği', `${(config.criticalTemperatureC ?? 0).toFixed(1)} °C`],
    ['Kritik Kapasite Eşiği', `${config.criticalCapacityPercent ?? 0} %`],
  ] : [];

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-slate-800 border border-slate-600 rounded-xl p-6 max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-slate-100 font-semibold">UPS Konfigürasyonu</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-200 text-xl leading-none">✕</button>
        </div>

        <p className="text-slate-500 text-xs mb-3">
          Bu cihazda SNMP konfigürasyon yazma desteklenmiyor — değerler salt okunur.
        </p>

        {loading && <p className="text-slate-400 text-sm">Yükleniyor...</p>}
        {error && <p className="text-red-400 text-sm bg-red-900/20 border border-red-800 rounded p-2 mb-3">{error}</p>}

        {config && (
          <dl className="space-y-2">
            {rows.map(([label, value]) => (
              <div key={label} className="flex justify-between items-center border-b border-slate-700 pb-1.5">
                <dt className="text-slate-400 text-xs">{label}</dt>
                <dd className="text-slate-200 text-sm font-medium">{value}</dd>
              </div>
            ))}
          </dl>
        )}

        <div className="flex justify-end pt-4">
          <button onClick={onClose} className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded-lg text-sm transition-colors">
            Kapat
          </button>
        </div>
      </div>
    </div>
  );
}
