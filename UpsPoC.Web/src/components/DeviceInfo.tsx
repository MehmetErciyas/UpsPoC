import type { UpsStatus } from '../types';

interface Props {
  status: UpsStatus | null;
}

export default function DeviceInfo({ status }: Props) {
  const rows: [string, string][] = [
    ['Sistem Adı', status?.systemName || '—'],
    ['Açıklama', status?.systemDescription || '—'],
    ['Donanım Versiyonu', status?.hardwareVersion || '—'],
    ['Firmware', status?.firmwareVersion || '—'],
    ['Seri Numarası', status?.serialNumber || '—'],
    ['Konum', status?.location || '—'],
    ['Bağlantı Kişisi', status?.contact || '—'],
    ['Çalışma Süresi', status?.uptimeText || '—'],
    ['Sistem Saati', status?.systemTime || '—'],
    ['Son Test', status?.lastTestResultText || '—'],
    ['Sonraki Test', status?.nextTestSchedule || '—'],
    ['Kapanma Uyarısı', status?.shutdownWarning || '—'],
    ['Günlük Rapor E-posta', status?.dailyReportEmail || '—'],
    ['Akü Adedi', status ? `${status.batteryBlockCount ?? 0} blok` : '—'],
    ['Batarya Paket V', status ? `${(status.batteryPackVoltage ?? 0).toFixed(1)} V` : '—'],
    ['Çıkış Voltajı', status ? `${(status.outputVoltage ?? 0).toFixed(1)} VAC` : '—'],
    ['Çıkış Frekansı', status ? `${(status.outputFrequency ?? 0).toFixed(1)} Hz` : '—'],
  ];

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Cihaz Bilgisi</h3>
      <dl className="space-y-1.5">
        {rows.map(([label, value]) => (
          <div key={label} className="flex justify-between items-center">
            <dt className="text-slate-400 text-xs">{label}</dt>
            <dd className="text-slate-200 text-xs font-medium text-right max-w-[60%] truncate" title={value}>{value}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
