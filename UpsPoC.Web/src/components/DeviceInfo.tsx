import type { UpsStatus } from '../types';

interface Props {
  status: UpsStatus | null;
}

export default function DeviceInfo({ status }: Props) {
  const rows: [string, string][] = [
    ['Model', status?.modelName || '—'],
    ['Firmware', status?.firmwareVersion || '—'],
    ['Çıkış Voltajı', status ? `${status.outputVoltage} VAC` : '—'],
    ['Çıkış Frekansı', status ? `${status.outputFrequency} Hz` : '—'],
    ['Bağlı Cihazlar', status?.attachedDevices || '—'],
  ];

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Cihaz Bilgisi</h3>
      <dl className="space-y-1.5">
        {rows.map(([label, value]) => (
          <div key={label} className="flex justify-between items-center">
            <dt className="text-slate-400 text-xs">{label}</dt>
            <dd className="text-slate-200 text-xs font-medium text-right max-w-[60%] truncate">{value}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
