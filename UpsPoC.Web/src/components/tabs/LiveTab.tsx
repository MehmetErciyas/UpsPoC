import MetricCard from '../MetricCard';
import PowerChart from '../PowerChart';
import CommandPanel from '../CommandPanel';
import AlarmPanel from '../AlarmPanel';
import DeviceInfo from '../DeviceInfo';
import type { UpsStatus, UpsSnapshot } from '../../types';

interface Props {
  status: UpsStatus | null;
  history: UpsSnapshot[];
}

export default function LiveTab({ status, history }: Props) {
  const dimmed = !status?.isConnected;
  const batteryProgress = status?.batteryCapacityPercent ?? 0;

  return (
    <>
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
          value={status ? `${status.batteryRemainingMinutes ?? 0}dk` : '—'}
          subValue={status ? `${(status.batteryPackVoltage ?? 0).toFixed(1)}V (${(status.batteryVoltagePerCell ?? 0).toFixed(2)}V/cell × ${status.batteryBlockCount ?? 0})` : undefined}
          color="blue"
          progress={status ? Math.min(100, ((status.batteryRemainingMinutes ?? 0) / 60) * 100) : 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Yük"
          value={status ? `${status.outputLoadPercent ?? 0}%` : '—'}
          subValue={status ? `Çıkış ${(status.outputVoltage ?? 0).toFixed(1)}V / ${(status.outputFrequency ?? 0).toFixed(1)}Hz` : undefined}
          color="yellow"
          progress={status?.outputLoadPercent ?? 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Giriş"
          value={status ? `${(status.inputVoltage ?? 0).toFixed(1)}V` : '—'}
          subValue={status ? `${(status.inputFrequency ?? 0).toFixed(1)} Hz` : undefined}
          color="purple"
          progress={status ? Math.min(100, ((status.inputVoltage ?? 0) / 250) * 100) : 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Sıcaklık"
          value={status ? `${(status.batteryTemperature ?? 0).toFixed(1)}°C` : '—'}
          subValue="Batarya"
          color="orange"
          progress={status ? Math.min(100, ((status.batteryTemperature ?? 0) / 60) * 100) : 0}
          dimmed={dimmed}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        <div className="lg:col-span-2">
          <PowerChart history={history} />
        </div>
        <CommandPanel />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <AlarmPanel status={status} />
        <DeviceInfo status={status} />
      </div>
    </>
  );
}
