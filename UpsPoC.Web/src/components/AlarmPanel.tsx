import type { UpsStatus } from '../types';

interface Props {
  status: UpsStatus | null;
}

export default function AlarmPanel({ status }: Props) {
  const alarmCount = status?.activeAlarmCount ?? 0;
  const isOnBattery = status?.outputSource === 4;

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Alarmlar & Durum</h3>
      <div className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Aktif Alarm</span>
          <span className={`text-sm font-semibold ${alarmCount > 0 ? 'text-red-400' : 'text-emerald-400'}`}>
            {alarmCount > 0 ? `⚠ ${alarmCount} alarm` : '✓ Alarm yok'}
          </span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Güç Kaynağı</span>
          <span className={`text-sm font-semibold ${isOnBattery ? 'text-amber-400' : 'text-emerald-400'}`}>
            {isOnBattery ? '⚡ ' : ''}{status?.outputSourceText ?? '—'}
          </span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Batarya Durumu</span>
          <span className={`text-sm font-semibold ${
            status?.batteryStatus === 2 ? 'text-emerald-400' :
            status?.batteryStatus === 3 ? 'text-amber-400' :
            status?.batteryStatus === 4 ? 'text-red-400' : 'text-slate-400'
          }`}>
            {status?.batteryStatusText ?? '—'}
          </span>
        </div>
      </div>
    </div>
  );
}
