import { useState } from 'react';
import { api } from '../api/client';
import { useUpsData } from '../hooks/useUpsData';
import MetricCard from '../components/MetricCard';
import PowerChart from '../components/PowerChart';
import CommandPanel from '../components/CommandPanel';
import AlarmPanel from '../components/AlarmPanel';
import DeviceInfo from '../components/DeviceInfo';
import ConfigModal from '../components/ConfigModal';

interface Props {
  onLogout: () => void;
}

const INTERVAL_OPTIONS = [5, 10, 15, 30, 60];

export default function Dashboard({ onLogout }: Props) {
  const [intervalSec, setIntervalSec] = useState(5);
  const [showConfig, setShowConfig] = useState(false);
  const { status, history, isLoading, error } = useUpsData(intervalSec);

  const handleLogout = async () => {
    await api.auth.logout().catch(() => {});
    onLogout();
  };

  const dimmed = !status?.isConnected;
  const batteryProgress = status?.batteryCapacityPercent ?? 0;

  return (
    <div className="min-h-screen bg-slate-900 p-4">
      {/* Top Bar */}
      <div className="flex justify-between items-center mb-5 pb-4 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <span className="text-sky-400 text-xl font-bold">⚡ UPS Monitor</span>
          <span className="text-slate-500 text-sm">EA900 · 192.168.143.246</span>
        </div>
        <div className="flex items-center gap-4">
          {isLoading && <span className="text-slate-500 text-xs animate-pulse">Bağlanıyor...</span>}
          {!isLoading && status?.isConnected && (
            <span className="bg-emerald-950 text-emerald-400 border border-emerald-800 rounded-full px-3 py-0.5 text-xs">● ONLİNE</span>
          )}
          {!isLoading && !status?.isConnected && (
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
          <button onClick={() => setShowConfig(true)} className="text-xs text-slate-400 hover:text-slate-200 transition-colors">
            ⚙ Ayarlar
          </button>
          <button onClick={handleLogout} className="text-xs text-slate-400 hover:text-slate-200 transition-colors">
            🔓 Çıkış
          </button>
        </div>
      </div>

      {/* Error banner */}
      {error && (
        <div className="bg-red-900/30 border border-red-800 text-red-300 text-sm rounded-lg px-4 py-2 mb-4">
          ⚠ {error}
        </div>
      )}

      {/* Metric Cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3 mb-4">
        <MetricCard
          label="Batarya"
          value={`${batteryProgress}%`}
          subValue={status?.batteryStatusText}
          color="green"
          progress={batteryProgress}
          dimmed={dimmed}
        />
        <MetricCard
          label="Kalan Süre"
          value={status ? `${status.batteryRemainingMinutes}dk` : '—'}
          subValue={status ? `${status.batteryVoltage.toFixed(1)}V DC` : undefined}
          color="blue"
          progress={status ? Math.min(100, (status.batteryRemainingMinutes / 60) * 100) : 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Yük"
          value={status ? `${status.outputLoadPercent}%` : '—'}
          subValue={status ? `${status.outputPowerWatts}W` : undefined}
          color="yellow"
          progress={status?.outputLoadPercent ?? 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Giriş"
          value={status ? `${status.inputVoltage}V` : '—'}
          subValue={status ? `${status.inputFrequency.toFixed(1)} Hz` : undefined}
          color="purple"
          progress={status ? Math.min(100, (status.inputVoltage / 250) * 100) : 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Sıcaklık"
          value={status ? `${status.batteryTemperature}°C` : '—'}
          subValue="Batarya"
          color="orange"
          progress={status ? Math.min(100, (status.batteryTemperature / 60) * 100) : 0}
          dimmed={dimmed}
        />
      </div>

      {/* Chart + Commands */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        <div className="lg:col-span-2">
          <PowerChart history={history} />
        </div>
        <CommandPanel />
      </div>

      {/* Alarm + Device Info */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <AlarmPanel status={status} />
        <DeviceInfo status={status} />
      </div>

      {showConfig && <ConfigModal onClose={() => setShowConfig(false)} />}
    </div>
  );
}
