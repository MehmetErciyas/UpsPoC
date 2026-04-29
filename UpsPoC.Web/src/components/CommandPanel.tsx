import { useState, useEffect, useRef } from 'react';
import { api } from '../api/client';
import ConfirmDialog from './ConfirmDialog';
import type { UpsCommand } from '../types';

interface PendingCmd {
  title: string;
  message: string;
  command: UpsCommand;
}

interface Toast {
  message: string;
  type: 'success' | 'error';
}

export default function CommandPanel() {
  const [pending, setPending] = useState<PendingCmd | null>(null);
  const [toast, setToast] = useState<Toast | null>(null);
  const [shutdownDelay, setShutdownDelay] = useState(60);
  const [rebootDelay, setRebootDelay] = useState(60);
  const toastTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const showToast = (message: string, type: 'success' | 'error') => {
    setToast({ message, type });
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    toastTimerRef.current = setTimeout(() => setToast(null), 4000);
  };

  useEffect(() => {
    return () => {
      if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    };
  }, []);

  const executeCommand = async (cmd: UpsCommand) => {
    try {
      await api.ups.sendCommand(cmd);
      showToast(`Komut gönderildi: ${cmd.commandName}`, 'success');
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Komut gönderilemedi', 'error');
    }
  };

  const confirmThen = (title: string, message: string, cmd: UpsCommand) => {
    setPending({ title, message, command: cmd });
  };

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-4">Komutlar</h3>

      <p className="text-slate-500 text-xs uppercase tracking-wider mb-2">Güç Kontrolü</p>
      <div className="space-y-2 mb-4">
        <div className="flex items-center gap-2">
          <input
            type="number"
            value={shutdownDelay}
            onChange={e => setShutdownDelay(Number(e.target.value))}
            className="w-16 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-xs text-slate-200"
            min={1}
          />
          <span className="text-slate-500 text-xs">sn</span>
          <button
            onClick={() => confirmThen(
              'UPS Kapatılıyor',
              `UPS ${shutdownDelay} saniye sonra kapatılacak. Bağlı cihazlar etkilenecek. Emin misiniz?`,
              { commandName: 'shutdown-after-delay', intValue: shutdownDelay }
            )}
            className="flex-1 bg-red-900/30 border border-red-800 text-red-400 hover:bg-red-900/50 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
          >
            ⬛ Gecikmeli Kapat
          </button>
        </div>
        <div className="flex items-center gap-2">
          <input
            type="number"
            value={rebootDelay}
            onChange={e => setRebootDelay(Number(e.target.value))}
            className="w-16 bg-slate-700 border border-slate-600 rounded px-2 py-1 text-xs text-slate-200"
            min={0}
            max={300}
          />
          <span className="text-slate-500 text-xs">sn</span>
          <button
            onClick={() => confirmThen(
              'UPS Yeniden Başlatılıyor',
              `UPS kapatılıp ${rebootDelay} saniye sonra yeniden başlatılacak. Emin misiniz?`,
              { commandName: 'reboot', intValue: rebootDelay }
            )}
            className="flex-1 bg-amber-900/30 border border-amber-800 text-amber-400 hover:bg-amber-900/50 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
          >
            🔄 Yeniden Başlat
          </button>
        </div>
        <button
          onClick={() => confirmThen(
            'Kapatmayı İptal Et',
            'Aktif kapatma geri sayımı iptal edilecek. Emin misiniz?',
            { commandName: 'abort-shutdown' }
          )}
          className="w-full bg-slate-700 border border-slate-600 text-slate-300 hover:bg-slate-600 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          ✕ Kapatmayı İptal Et
        </button>
      </div>

      <p className="text-slate-500 text-xs uppercase tracking-wider mb-2">Test & Ayar</p>
      <div className="space-y-2">
        <button
          onClick={() => executeCommand({ commandName: 'battery-test' })}
          className="w-full bg-emerald-900/30 border border-emerald-800 text-emerald-400 hover:bg-emerald-900/50 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          🔋 Batarya Testi Başlat
        </button>
        <button
          onClick={() => executeCommand({ commandName: 'audible-alarm', intValue: 2 })}
          className="w-full bg-sky-900/30 border border-sky-800 text-sky-400 hover:bg-sky-900/50 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          🔔 Alarm Aç
        </button>
        <button
          onClick={() => executeCommand({ commandName: 'audible-alarm', intValue: 1 })}
          className="w-full bg-slate-700 border border-slate-600 text-slate-300 hover:bg-slate-600 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          🔕 Alarm Kapat
        </button>
        <button
          onClick={() => executeCommand({ commandName: 'audible-alarm', intValue: 3 })}
          className="w-full bg-slate-700 border border-slate-600 text-slate-300 hover:bg-slate-600 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          🔇 Alarm Geçici Sessiz
        </button>
        <button
          onClick={() => executeCommand({ commandName: 'auto-restart', intValue: 1 })}
          className="w-full bg-violet-900/30 border border-violet-800 text-violet-400 hover:bg-violet-900/50 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          ↺ Oto-Başlatma Aç
        </button>
        <button
          onClick={() => executeCommand({ commandName: 'auto-restart', intValue: 2 })}
          className="w-full bg-slate-700 border border-slate-600 text-slate-300 hover:bg-slate-600 rounded-lg px-3 py-1.5 text-xs text-left transition-colors"
        >
          ⊘ Oto-Başlatma Kapat
        </button>
      </div>

      {pending && (
        <ConfirmDialog
          title={pending.title}
          message={pending.message}
          onConfirm={async () => { setPending(null); await executeCommand(pending.command); }}
          onCancel={() => setPending(null)}
        />
      )}

      {toast && (
        <div className={`fixed bottom-4 right-4 z-50 px-4 py-3 rounded-lg text-sm shadow-lg border ${
          toast.type === 'success'
            ? 'bg-emerald-900/90 border-emerald-700 text-emerald-200'
            : 'bg-red-900/90 border-red-700 text-red-200'
        }`}>
          {toast.message}
        </div>
      )}
    </div>
  );
}
