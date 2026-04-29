import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  Legend, ResponsiveContainer
} from 'recharts';
import type { UpsSnapshot } from '../types';

interface Props {
  history: UpsSnapshot[];
}

function formatTime(iso: string) {
  const d = new Date(iso);
  return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

export default function PowerChart({ history }: Props) {
  const data = history.map(s => ({
    time: formatTime(s.timestamp),
    'Yük %': s.outputLoadPercent,
    'Batarya %': s.batteryPercent,
    'Giriş V': s.inputVoltage,
  }));

  if (data.length === 0) {
    return (
      <div className="bg-slate-800 rounded-xl p-4 border border-slate-700 flex items-center justify-center h-52">
        <p className="text-slate-500 text-sm">Geçmiş veri toplanıyor...</p>
      </div>
    );
  }

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Güç Geçmişi</h3>
      <ResponsiveContainer width="100%" height={180}>
        <LineChart data={data} margin={{ top: 4, right: 8, left: -20, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
          <XAxis
            dataKey="time"
            tick={{ fill: '#64748b', fontSize: 10 }}
            interval="preserveStartEnd"
          />
          <YAxis tick={{ fill: '#64748b', fontSize: 10 }} />
          <Tooltip
            contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: 8 }}
            labelStyle={{ color: '#94a3b8' }}
            itemStyle={{ color: '#e2e8f0' }}
          />
          <Legend
            wrapperStyle={{ fontSize: 11, paddingTop: 8 }}
            formatter={(value) => <span style={{ color: '#94a3b8' }}>{value}</span>}
          />
          <Line type="monotone" dataKey="Yük %" stroke="#f59e0b" dot={false} strokeWidth={2} />
          <Line type="monotone" dataKey="Batarya %" stroke="#22c55e" dot={false} strokeWidth={2} />
          <Line type="monotone" dataKey="Giriş V" stroke="#a78bfa" dot={false} strokeWidth={2} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
