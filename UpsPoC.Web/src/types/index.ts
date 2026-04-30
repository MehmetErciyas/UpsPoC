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
  systemTime: string;
  nextTestSchedule: string;
  shutdownWarning: string;
  dailyReportEmail: string;

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
  manualBatteryBlockCount?: number | null;
}

export interface UpsConnectionInfo {
  host: string;
  port: number;
  readCommunity: string;
  hasWriteCommunity: boolean;
  isConfigured: boolean;
  manualBatteryBlockCount?: number | null;
}

export interface MetricDetail {
  key: string;
  title: string;
  group: 'live' | 'info' | 'f-equivalent';
  valueText: string;
  rawValue: string;
  oid: string;
  ok: boolean;
  error?: string;
}

export interface DiagnosticLine {
  title: string;
  oid: string;
  ok: boolean;
  value?: string;
  error?: string;
}

export interface DiagnosticResult {
  lines: DiagnosticLine[];
  hints: string[];
}

export interface RawOidResult {
  oid: string;
  type: string;
  value: string;
}
