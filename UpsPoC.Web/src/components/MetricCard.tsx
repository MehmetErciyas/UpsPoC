interface Props {
  label: string;
  value: string;
  subValue?: string;
  color: 'green' | 'blue' | 'yellow' | 'purple' | 'orange';
  progress?: number;
  dimmed?: boolean;
}

const colorMap = {
  green:  { text: 'text-emerald-400', bar: 'bg-emerald-500', track: 'bg-emerald-950' },
  blue:   { text: 'text-sky-400',     bar: 'bg-sky-500',     track: 'bg-sky-950' },
  yellow: { text: 'text-amber-400',   bar: 'bg-amber-500',   track: 'bg-amber-950' },
  purple: { text: 'text-violet-400',  bar: 'bg-violet-500',  track: 'bg-violet-950' },
  orange: { text: 'text-orange-400',  bar: 'bg-orange-500',  track: 'bg-orange-950' },
};

export default function MetricCard({ label, value, subValue, color, progress, dimmed }: Props) {
  const c = colorMap[color];
  return (
    <div className={`bg-slate-800 rounded-xl p-4 border border-slate-700 transition-opacity ${dimmed ? 'opacity-40' : ''}`}>
      <p className="text-slate-500 text-xs uppercase tracking-wider mb-2">{label}</p>
      <p className={`text-2xl font-bold ${c.text}`}>{value}</p>
      {progress !== undefined && (
        <div className={`${c.track} h-1 rounded-full mt-2`}>
          <div
            className={`${c.bar} h-1 rounded-full transition-all duration-500`}
            style={{ width: `${Math.min(100, Math.max(0, progress))}%` }}
          />
        </div>
      )}
      {subValue && <p className="text-slate-500 text-xs mt-1">{subValue}</p>}
    </div>
  );
}
