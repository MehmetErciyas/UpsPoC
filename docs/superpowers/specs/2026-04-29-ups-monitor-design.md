# UPS Monitor Web Uygulaması — Tasarım Dokümanı

**Tarih:** 2026-04-29  
**Kapsam:** East EA900 UPS + NetAgent 9 MiniGo SNMP kartı için web tabanlı izleme ve kontrol paneli  
**Hedef IP:** 192.168.143.246  

---

## 1. Genel Bakış

East EA900 UPS'e takılı NetAgent 9 MiniGo SNMP ağ kartı üzerinden RFC 1628 UPS MIB protokolü ile iletişim kuran, anlık verileri gösteren ve komut gönderebilen bir web uygulaması.

---

## 2. Mimari

### Proje Yapısı

```
UpsPoC/
├── UpsPoC.Api/              # ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── UpsController.cs
│   ├── Services/
│   │   ├── SnmpService.cs       # SNMP GET/SET işlemleri
│   │   └── UpsDataService.cs    # In-memory geçmiş, polling koordinasyonu
│   ├── Models/
│   │   ├── UpsStatus.cs
│   │   ├── UpsSnapshot.cs
│   │   ├── UpsCommand.cs
│   │   └── AppSettings.cs
│   └── appsettings.json
└── UpsPoC.Web/              # React + Vite + TypeScript
    ├── src/
    │   ├── components/
    │   │   ├── MetricCard.tsx
    │   │   ├── PowerChart.tsx
    │   │   ├── CommandPanel.tsx
    │   │   ├── AlarmPanel.tsx
    │   │   ├── ConfigModal.tsx
    │   │   └── ConfirmDialog.tsx
    │   ├── pages/
    │   │   ├── Dashboard.tsx
    │   │   └── Login.tsx
    │   ├── hooks/
    │   │   └── useUpsData.ts    # Polling hook
    │   └── api/
    │       └── client.ts        # Fetch wrapper
    └── vite.config.ts
```

### Veri Akışı

```
React (polling her N sn)
  → GET /api/ups/status
  → SnmpService.GetAsync(OIDs)
  → UDP 161 → NetAgent 9 MiniGo
  → RFC 1628 OID değerleri
  → JSON response → UI render

React (komut)
  → POST /api/ups/command
  → SnmpService.SetAsync(OID, value)
  → UDP 161 → NetAgent 9 MiniGo
```

---

## 3. SNMP Verileri

### Okunacak OID'ler (RFC 1628)

| Kategori | Veri | OID |
|----------|------|-----|
| Kimlik | Model adı | `1.3.6.1.2.1.33.1.1.2.0` |
| Kimlik | Firmware versiyonu | `1.3.6.1.2.1.33.1.1.4.0` |
| Kimlik | Bağlı cihazlar | `1.3.6.1.2.1.33.1.1.6.0` |
| Batarya | Durum (1=bilinmiyor, 2=normal, 3=düşük, 4=kritik) | `1.3.6.1.2.1.33.1.2.1.0` |
| Batarya | Kalan süre (dakika) | `1.3.6.1.2.1.33.1.2.3.0` |
| Batarya | Voltaj (0.1V DC) | `1.3.6.1.2.1.33.1.2.5.0` |
| Batarya | Sıcaklık (°C) | `1.3.6.1.2.1.33.1.2.7.0` |
| Giriş | Frekans (0.1 Hz) | `1.3.6.1.2.1.33.1.3.3.1.2.1` |
| Giriş | Voltaj (VAC) | `1.3.6.1.2.1.33.1.3.3.1.3.1` |
| Çıkış | Kaynak (1=diğer,2=normal,3=bypass,4=batarya,5=booster,6=reducer) | `1.3.6.1.2.1.33.1.4.1.0` |
| Çıkış | Frekans (0.1 Hz) | `1.3.6.1.2.1.33.1.4.2.0` |
| Çıkış | Voltaj (VAC) | `1.3.6.1.2.1.33.1.4.4.1.2.1` |
| Çıkış | Akım (0.1A) | `1.3.6.1.2.1.33.1.4.4.1.3.1` |
| Çıkış | Yük yüzdesi | `1.3.6.1.2.1.33.1.4.4.1.5.1` |
| Çıkış | Gerçek güç (W) | `1.3.6.1.2.1.33.1.4.4.1.7.1` |
| Alarm | Aktif alarm sayısı | `1.3.6.1.2.1.33.1.6.1.0` |

---

## 4. Komutlar (SNMP SET)

### Tehlikeli — Onay dialogu gerekir

| Komut | OID | Değer | Açıklama |
|-------|-----|-------|---------|
| Kapatma tipi | `1.3.6.1.2.1.33.1.8.1` | 1=output, 2=sistem | Neyin kapanacağını belirler |
| Gecikmeli kapat | `1.3.6.1.2.1.33.1.8.2` | saniye (-1=iptal) | X sn sonra kapat |
| Gecikmeli başlat | `1.3.6.1.2.1.33.1.8.3` | saniye (-1=iptal) | X sn sonra başlat |
| Yeniden başlat | `1.3.6.1.2.1.33.1.8.4` | saniye (0-300) | Kapat + otomatik başlat |

### Normal — Doğrudan çalışır

| Komut | OID | Değer | Açıklama |
|-------|-----|-------|---------|
| Otomatik başlatma | `1.3.6.1.2.1.33.1.8.5` | 1=açık, 2=kapalı | Güç sonrası oto-başlatma |
| Batarya testi | `1.3.6.1.2.1.33.1.7.1` | test OID | Yerinde batarya testi |
| Sesli alarm | `1.3.6.1.2.1.33.1.9.8` | 1=kapalı, 2=açık, 3=geçici sessiz | Alarm sesi kontrolü |
| UPS adı | `1.3.6.1.2.1.33.1.1.5` | string (max 63 karakter) | Cihaz ismi |

### Konfigürasyon (modal ile)

| Ayar | OID | Birim |
|------|-----|-------|
| Nominal giriş voltajı | `1.3.6.1.2.1.33.1.9.1` | RMS Volt |
| Nominal giriş frekansı | `1.3.6.1.2.1.33.1.9.2` | 0.1 Hz |
| Nominal çıkış voltajı | `1.3.6.1.2.1.33.1.9.3` | RMS Volt |
| Nominal çıkış frekansı | `1.3.6.1.2.1.33.1.9.4` | 0.1 Hz |
| Düşük batarya eşiği | `1.3.6.1.2.1.33.1.9.7` | Dakika |
| Düşük voltaj transfer | `1.3.6.1.2.1.33.1.9.9` | RMS Volt |
| Yüksek voltaj transfer | `1.3.6.1.2.1.33.1.9.10` | RMS Volt |

---

## 5. UI Tasarımı

**Stil:** Modern Dark Pro (koyu tema, `#0f172a` arka plan, Tailwind CSS)

### Dashboard Bileşenleri

- **Üst bar:** UPS adı, IP, bağlantı durumu badge, ayarlanabilir polling süresi seçici, çıkış butonu
- **5 Metrik Kart:** Batarya %, Kalan Süre (dakika + voltaj), Yük % (W/W), Giriş V + Hz, Sıcaklık — her birinde renk kodlu progress bar
- **Güç Geçmişi Grafiği:** Recharts LineChart, in-memory son 720 nokta (~1 saat, 5sn polling), Yük/Batarya/Giriş voltajı çizgileri
- **Komut Paneli:** Güç kontrolü grubu (onay dialoglu) + Test & Ayar grubu
- **Alarm Paneli:** Aktif alarm sayısı ve durumu
- **Cihaz Bilgisi:** Model, firmware, çıkış voltajı, oto-başlatma durumu

### Ek Sayfalar

- **Login:** Kullanıcı adı + şifre formu, cookie-based session
- **Konfigürasyon Modalı:** Voltaj/frekans eşik ayarları (tüm upsConfig OID'leri)

---

## 6. Teknik Stack

### Backend

| Bileşen | Teknoloji |
|---------|-----------|
| Framework | ASP.NET Core 8 Web API |
| SNMP | `Lextm.SharpSnmpLib` v12.5.7 |
| Auth | Cookie auth + `BCrypt.Net-Next` |
| Config | `IOptions<AppSettings>` |
| History | `ConcurrentQueue<UpsSnapshot>` (max 720) |
| Hosting | `http://0.0.0.0:5000` |

### Frontend

| Bileşen | Teknoloji |
|---------|-----------|
| Framework | React 18 + TypeScript |
| Build | Vite 5 |
| Stil | Tailwind CSS v3 |
| Grafikler | Recharts |
| HTTP | native `fetch` |
| State | React hooks |

---

## 7. Konfigürasyon (appsettings.json)

```json
{
  "Ups": {
    "Host": "192.168.143.246",
    "Port": 161,
    "ReadCommunity": "public",
    "WriteCommunity": "private",
    "TimeoutMs": 3000,
    "DefaultPollingIntervalSeconds": 5
  },
  "Auth": {
    "Username": "admin",
    "PasswordHash": "<bcrypt-hash>"
  }
}
```

---

## 8. Hata Yönetimi

- **SNMP timeout (3sn):** UI'da "Bağlantı Yok" banner'ı, son bilinen değerler gri gösterilir
- **SNMP SET hatası:** Toast notification ("Komut gönderilemedi: [hata]")
- **Auth hatası (401):** Login sayfasına otomatik yönlendirme
- **Ağ hatası:** Polling devam eder, kullanıcıya durum bilgisi verilir

---

## 9. Deployment

```bash
# Development
cd UpsPoC.Api && dotnet run --urls "http://0.0.0.0:5000"
cd UpsPoC.Web && npm run dev   # proxy → :5000

# Production (tek port)
cd UpsPoC.Web && npm run build
# dist/ → UpsPoC.Api/wwwroot/ kopyalanır
cd UpsPoC.Api && dotnet run --urls "http://0.0.0.0:5000"
# Hem API hem UI tek port üzerinden
```

---

## 10. Kısıtlar ve Notlar

- Megatec NetAgent 9 MiniGo yalnızca RFC 1628 standart MIB'i destekler; vendor-specific ek OID'ler kamuoyunda belgelenmemiş
- SNMP community string'leri `appsettings.json`'da tutulur — production'da güvenli config yönetimi önerilir
- In-memory geçmiş uygulama yeniden başlatıldığında sıfırlanır (kalıcı geçmiş bu sürümde kapsam dışı)
- SNMPv2c kullanılır; v3 (auth+encrypt) bu sürümde kapsam dışı
