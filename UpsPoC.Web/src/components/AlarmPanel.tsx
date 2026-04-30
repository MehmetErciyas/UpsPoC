import type { UpsStatus } from '../types';

interface Props {
  status: UpsStatus | null;
}

// Python netagent_gui_v10 alarm_control benzeri client-side kural değerlendirmesi.
function deriveAlarms(status: UpsStatus | null): string[] {
  if (!status || !status.isConnected) return [];
  const alarms: string[] = [];

  if (status.inputVoltage > 0 && status.inputVoltage < 180)
    alarms.push('Giriş voltajı düşük veya şebeke yok.');

  if (status.batteryCapacityPercent > 0 && status.batteryCapacityPercent <= 30)
    alarms.push('Batarya kapasitesi düşük.');

  if (status.outputLoadPercent >= 90)
    alarms.push('UPS yük oranı yüksek.');

  if (status.outputSource === 3)
    alarms.push('UPS aküden çalışıyor.');

  if (status.batteryStatus === 3)
    alarms.push('Batarya düşük alarmı.');

  if (status.batteryTemperature >= 50)
    alarms.push('Batarya sıcaklığı yüksek.');

  return alarms;
}

export default function AlarmPanel({ status }: Props) {
  const alarms = deriveAlarms(status);
  const isOnBattery = status?.outputSource === 3;

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Alarmlar & Durum</h3>
      <div className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Aktif Alarm</span>
          <span className={`text-sm font-semibold ${alarms.length > 0 ? 'text-red-400' : 'text-emerald-400'}`}>
            {alarms.length > 0 ? `⚠ ${alarms.length} alarm` : '✓ Alarm yok'}
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
            status?.batteryStatus === 3 ? 'text-amber-400' : 'text-slate-400'
          }`}>
            {status?.batteryStatusText ?? '—'}
          </span>
        </div>

        {alarms.length > 0 && (
          <ul className="pt-2 mt-2 border-t border-slate-700 space-y-1">
            {alarms.map((a, i) => (
              <li key={i} className="text-xs text-red-300">⚠ {a}</li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
