export interface UpsStatus {
  modelName: string;
  firmwareVersion: string;
  attachedDevices: string;
  batteryStatus: number;
  batteryStatusText: string;
  batteryCapacityPercent: number;
  batteryRemainingMinutes: number;
  batteryVoltage: number;
  batteryTemperature: number;
  inputVoltage: number;
  inputFrequency: number;
  outputSource: number;
  outputSourceText: string;
  outputFrequency: number;
  outputVoltage: number;
  outputCurrent: number;
  outputLoadPercent: number;
  outputPowerWatts: number;
  activeAlarmCount: number;
  timestamp: string;
  isConnected: boolean;
  errorMessage?: string;
}

export interface UpsSnapshot {
  timestamp: string;
  batteryPercent: number;
  outputLoadPercent: number;
  inputVoltage: number;
  outputVoltage: number;
  batteryRemainingMinutes: number;
  outputPowerWatts: number;
}

export interface UpsConfig {
  inputVoltageNominal: number;
  inputFreqNominal: number;
  outputVoltageNominal: number;
  outputFreqNominal: number;
  lowBatteryMinutes: number;
  audibleStatus: number;
  lowVoltageTransferPoint: number;
  highVoltageTransferPoint: number;
}

export interface UpsCommand {
  commandName: string;
  intValue?: number;
  stringValue?: string;
}
