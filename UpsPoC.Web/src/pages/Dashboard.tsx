import { useEffect, useState } from 'react';
import { api } from '../api/client';
import { useUpsData } from '../hooks/useUpsData';
import ConfigModal from '../components/ConfigModal';
import ConnectionBar from '../components/ConnectionBar';
import LiveTab from '../components/tabs/LiveTab';
import MetricsTableTab from '../components/tabs/MetricsTableTab';
import ControlTab from '../components/tabs/ControlTab';
import OidScanTab from '../components/tabs/OidScanTab';
import type { UpsConnectionInfo } from '../types';

interface Props {
  onLogout: () => void;
}

const INTERVAL_OPTIONS = [5, 10, 15, 30, 60];

type TabKey = 'live' | 'detail' | 'control' | 'scan';
const TABS: { key: TabKey; label: string }[] = [
  { key: 'live',    label: '📊 Canlı' },
  { key: 'detail',  label: '📋 Detay Tablo' },
  { key: 'control', label: '🎛 Komut & Diagnostic' },
  { key: 'scan',    label: '🔍 OID Tarama' },
];

export default function Dashboard({ onLogout }: Props) {
  const [intervalSec, setIntervalSec] = useState(5);
  const [showConfig, setShowConfig] = useState(false);
  const [connection, setConnection] = useState<UpsConnectionInfo | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>('live');

  useEffect(() => {
    api.ups.getConnection()
      .then(setConnection)
      .catch(() => setConnection({ host: '', port: 161, readCommunity: 'public', hasWriteCommunity: false, isConfigured: false }));
  }, []);

  const isConfigured = connection?.isConfigured ?? false;
  const { status, history, isLoading, error } = useUpsData(intervalSec, isConfigured);

  const handleLogout = async () => {
    await api.auth.logout().catch(() => {});
    onLogout();
  };

  return (
    <div className="min-h-screen bg-slate-900 p-4">
      {/* Top Bar */}
      <div className="flex justify-between items-center mb-5 pb-4 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <span className="text-sky-400 text-xl font-bold">⚡ UPS Monitor</span>
          {connection?.host && (
            <span className="text-slate-500 text-sm">
              {isConfigured ? `${connection.host}:${connection.port}` : 'Bağlı değil'}
            </span>
          )}
        </div>
        <div className="flex items-center gap-4">
          {!isConfigured && (
            <span className="bg-slate-800 text-slate-400 border border-slate-700 rounded-full px-3 py-0.5 text-xs">○ DURDU</span>
          )}
          {isConfigured && isLoading && <span className="text-slate-500 text-xs animate-pulse">Bağlanıyor...</span>}
          {isConfigured && !isLoading && status?.isConnected && (
            <span className="bg-emerald-950 text-emerald-400 border border-emerald-800 rounded-full px-3 py-0.5 text-xs">● ONLİNE</span>
          )}
          {isConfigured && !isLoading && !status?.isConnected && (
            <span className="bg-red-950 text-red-400 border border-red-800 rounded-full px-3 py-0.5 text-xs">● BAĞLANTI YOK</span>
          )}
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span>Yenileme:</span>
            <select
              value={intervalSec}
              onChange={e => setIntervalSec(Number(e.target.value))}
              className="bg-slate-800 border border-slate-700 rounded px-2 py-0.5 text-slate-300 focus:outline-none"
            >
              {INTERVAL_OPTIONS.map(s => <option key={s} value={s}>{s}s</option>)}
            </select>
          </div>
          <a
            href={api.ups.historyCsvUrl}
            download
            className={`text-xs text-slate-400 hover:text-slate-200 transition-colors ${!isConfigured || history.length === 0 ? 'opacity-30 pointer-events-none' : ''}`}
            title="Geçmiş veriyi CSV olarak indir"
          >
            ⬇ CSV
          </a>
          <button onClick={() => setShowConfig(true)} disabled={!isConfigured} className="text-xs text-slate-400 hover:text-slate-200 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
            ⚙ Eşikler
          </button>
          <button onClick={handleLogout} className="text-xs text-slate-400 hover:text-slate-200 transition-colors">
            🔓 Çıkış
          </button>
        </div>
      </div>

      <ConnectionBar connection={connection} onChange={setConnection} />

      {error && (
        <div className="bg-red-900/30 border border-red-800 text-red-300 text-sm rounded-lg px-4 py-2 mb-4">
          ⚠ {error}
        </div>
      )}

      {!isConfigured ? (
        <div className="bg-slate-800 border border-slate-700 rounded-xl p-12 text-center">
          <p className="text-slate-400 text-sm">
            UPS izlemeyi başlatmak için yukarıdan IP ve community bilgilerini girip <strong className="text-emerald-400">Bağlan</strong>'a tıklayın.
          </p>
        </div>
      ) : (
        <>
          {/* Tab navigation */}
          <div className="flex gap-1 mb-4 border-b border-slate-700">
            {TABS.map(t => (
              <button
                key={t.key}
                onClick={() => setActiveTab(t.key)}
                className={`px-4 py-2 text-sm font-medium transition-colors border-b-2 -mb-px ${
                  activeTab === t.key
                    ? 'text-sky-400 border-sky-400'
                    : 'text-slate-400 border-transparent hover:text-slate-200'
                }`}
              >
                {t.label}
              </button>
            ))}
          </div>

          {/* Tab content */}
          {activeTab === 'live'    && <LiveTab status={status} history={history} />}
          {activeTab === 'detail'  && <MetricsTableTab refreshKey={Math.floor((status?.timestamp ? new Date(status.timestamp).getTime() : 0) / (intervalSec * 1000))} />}
          {activeTab === 'control' && <ControlTab />}
          {activeTab === 'scan'    && <OidScanTab />}
        </>
      )}

      {showConfig && <ConfigModal onClose={() => setShowConfig(false)} />}
    </div>
  );
}
