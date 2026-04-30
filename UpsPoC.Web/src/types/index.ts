export interface UpsStatus {
  // Sistem bilgisi
  firmwareVersion: string;
  hardwareVersion: string;
  serialNumber: string;
  systemName: string;
  systemDescription: string;
  location: string;
  contact: string;
  uptimeText: string;
  lastTestResultText: string;

  // Batarya
  batteryStatus: number;
  batteryStatusText: string;
  batteryCapacityPercent: number;
  batteryRemainingMinutes: number;
  batteryVoltagePerCell: number;
  batteryPackVoltage: number;
  batteryBlockCount: number;
  batteryTemperature: number;

  // Giriş
  inputVoltage: number;
  inputFrequency: number;

  // Çıkış
  outputSource: number;
  outputSourceText: string;
  outputVoltage: number;
  outputFrequency: number;
  outputLoadPercent: number;

  // Metadata
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
}

export interface UpsConfig {
  criticalLoadPercent: number;
  criticalTemperatureC: number;
  criticalCapacityPercent: number;
  nominalOutputVoltage: number;
  nominalBatteryVoltage: number;
}

export interface UpsCommand {
  commandName: 'reboot' | 'shutdown';
}

export interface ConnectionRequest {
  host: string;
  port: number;
  readCommunity: string;
  writeCommunity?: string;
}

export interface UpsConnectionInfo {
  host: string;
  port: number;
  readCommunity: string;
  hasWriteCommunity: boolean;
  isConfigured: boolean;
}
