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
      <h3 className="text-slate-200 text-sm font-semibold mb-2">Komutlar</h3>
      <p className="text-slate-500 text-xs mb-4">
        Bu cihazda yalnızca NetAgent reboot/shutdown SET komutları desteklenir.
      </p>

      <p className="text-slate-500 text-xs uppercase tracking-wider mb-2">Güç Kontrolü</p>
      <div className="space-y-2 mb-4">
        <button
          onClick={() => confirmThen(
            'UPS Yeniden Başlatılıyor',
            'UPS reset/reboot komutu gönderilecek. Bağlı cihazlar kısa süreliğine etkilenebilir. Emin misiniz?',
            { commandName: 'reboot' }
          )}
          className="w-full bg-amber-900/30 border border-amber-800 text-amber-400 hover:bg-amber-900/50 rounded-lg px-3 py-2 text-xs text-left transition-colors"
        >
          🔄 UPS RESET / REBOOT (6.2.2)
        </button>

        <button
          onClick={() => confirmThen(
            'UPS Kapatılıyor',
            'UPS sleep/shutdown komutu gönderilecek. UPS çıkışı kapanabilir, bağlı cihazlar enerjisiz kalabilir. Emin misiniz?',
            { commandName: 'shutdown' }
          )}
          className="w-full bg-red-900/30 border border-red-800 text-red-400 hover:bg-red-900/50 rounded-lg px-3 py-2 text-xs text-left transition-colors"
        >
          ⬛ UPS SLEEP / KAPATMA (6.2.3)
        </button>
      </div>

      <p className="text-slate-500 text-[10px] italic">
        Diğer komutlar (battery test, audible alarm, oto-başlatma vb.) bu cihazda doğrulanmadığından
        backend tarafında 501 NotImplemented dönülür.
      </p>

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
