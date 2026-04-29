# UPS Monitor

East EA900 UPS'i **NetAgent 9 MiniGo SNMP** ağ kartı üzerinden izleyen ve kontrol eden web uygulaması.

![Dashboard](docs/assets/dashboard-preview.png)

## Özellikler

### İzleme
| Veri | Açıklama |
|------|---------|
| Batarya durumu | Normal / Düşük / Kritik + şarj yüzdesi |
| Kalan çalışma süresi | Dakika cinsinden |
| Batarya voltajı & sıcaklığı | DC volt, °C |
| Giriş voltajı & frekansı | VAC, Hz |
| Çıkış kaynağı | Normal / Batarya / Bypass / Booster / Reducer |
| Çıkış voltajı, akımı, yükü | VAC, A, % |
| Gerçek güç | W |
| Aktif alarm sayısı | RFC 1628 alarm tablosu |

### Komutlar
| Komut | Açıklama |
|-------|---------|
| Gecikmeli kapat | X saniye sonra UPS'i kapat |
| Yeniden başlat | Kapat + X saniye sonra başlat |
| Kapatmayı iptal et | Aktif geri sayımı durdur |
| Otomatik başlatma | Güç gelince oto-başlatmayı aç/kapat |
| Batarya testi | Yerinde batarya testi başlat |
| Sesli alarm | Aç / Kapat / Geçici sessiz |
| Konfigürasyon | Voltaj eşikleri, frekans, düşük batarya eşiği |

### Arayüz
- Modern koyu tema dashboard
- Ayarlanabilir polling süresi (5s, 10s, 15s, 30s, 60s)
- Son 1 saatlik güç geçmişi grafiği
- Tehlikeli komutlar için onay dialogu
- Tek port production deployment

---

## Gereksinimler

| Bileşen | Versiyon |
|---------|---------|
| .NET SDK | 8.0+ |
| Node.js | 20.19+ veya 22.x |
| UPS | East EA900 + NetAgent 9 MiniGo SNMP kartı |
| Ağ | UPS'e UDP 161 (SNMP) erişimi |

---

## Kurulum

### 1. Repoyu klonla

```bash
git clone https://github.com/MehmetErciyas/UpsPoC.git
cd UpsPoC
```

### 2. Backend bağımlılıklarını yükle

```bash
cd UpsPoC.Api
dotnet restore
```

### 3. Frontend bağımlılıklarını yükle

```bash
cd ../UpsPoC.Web
npm install
```

### 4. Şifreyi ayarla

`UpsPoC.Api/appsettings.json` dosyasını açın ve `PasswordHash` değerini değiştirin.

Yeni BCrypt hash üretmek için:

```csharp
// Geçici olarak Program.cs'e ekleyin, çalıştırın, hash'i kopyalayın, sonra silin
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("yeni-sifreniz"));
```

Varsayılan: kullanıcı adı `admin`, şifre `admin123`

---

## Çalıştırma

### Development modu (iki terminal)

**Terminal 1 — Backend:**
```bash
cd UpsPoC.Api
dotnet run --urls "http://0.0.0.0:5000"
```

**Terminal 2 — Frontend:**
```bash
cd UpsPoC.Web
npm run dev
```

Tarayıcıda **http://localhost:5173** adresini açın.

---

### Production modu (tek port)

```bash
# 1. Frontend build al
cd UpsPoC.Web
npm run build
cp -r dist/. ../UpsPoC.Api/wwwroot/

# 2. API'yi başlat
cd ../UpsPoC.Api
dotnet run --urls "http://0.0.0.0:5000"
```

Tarayıcıda **http://\<sunucu-ip\>:5000** adresini açın.

---

## Konfigürasyon

`UpsPoC.Api/appsettings.json`:

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

| Ayar | Açıklama | Varsayılan |
|------|---------|-----------|
| `Ups.Host` | UPS IP adresi | `192.168.143.246` |
| `Ups.Port` | SNMP UDP portu | `161` |
| `Ups.ReadCommunity` | SNMP okuma community | `public` |
| `Ups.WriteCommunity` | SNMP yazma community | `private` |
| `Ups.TimeoutMs` | SNMP zaman aşımı (ms) | `3000` |
| `Ups.DefaultPollingIntervalSeconds` | Varsayılan yenileme süresi | `5` |
| `Auth.Username` | Giriş kullanıcı adı | `admin` |
| `Auth.PasswordHash` | BCrypt şifre hash'i | — |

---

## Teknik Mimari

```
UpsPoC/
├── UpsPoC.Api/              # ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs    # Giriş/çıkış (cookie auth)
│   │   └── UpsController.cs     # UPS veri ve komut endpoint'leri
│   ├── Services/
│   │   ├── SnmpService.cs       # RFC 1628 SNMP GET/SET
│   │   └── UpsDataService.cs    # In-memory geçmiş (720 snapshot)
│   ├── Models/                  # AppSettings, UpsStatus, UpsConfig vb.
│   └── appsettings.json
└── UpsPoC.Web/              # React 19 + Vite 5 + TypeScript
    └── src/
        ├── api/client.ts        # API istemci
        ├── hooks/useUpsData.ts  # Polling hook
        ├── components/          # MetricCard, PowerChart, CommandPanel vb.
        └── pages/               # Dashboard, Login
```

**Kullanılan teknolojiler:**

| Katman | Teknoloji |
|--------|----------|
| Backend framework | ASP.NET Core 8 |
| SNMP kütüphanesi | Lextm.SharpSnmpLib 12.5.7 |
| Auth | Cookie auth + BCrypt.Net-Next |
| Frontend | React 19 + TypeScript + Vite 5 |
| Stil | Tailwind CSS 3 |
| Grafikler | Recharts |
| SNMP standardı | RFC 1628 UPS MIB |

---

## SNMP Bağlantı Testi

Uygulamayı başlatmadan önce SNMP bağlantısını doğrulayın:

```bash
# Linux/macOS
snmpget -v2c -c public 192.168.143.246 1.3.6.1.2.1.33.1.1.2.0

# Windows (snmpwalk kuruluysa)
snmpget -v2c -c public 192.168.143.246 1.3.6.1.2.1.33.1.1.2.0
```

Cevap geliyorsa (`STRING: "EA900"` gibi) SNMP çalışıyor demektir.

---

## Testler

```bash
cd UpsPoC.Api.Tests
dotnet test
```

6 unit test: `UpsDataService` in-memory geçmiş davranışları.

---

## Lisans

MIT
