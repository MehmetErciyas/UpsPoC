# UPS Monitor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** East EA900 UPS'i 192.168.143.246 IP'si üzerinden SNMP ile izleyen ve kontrol eden, C# backend + React frontend web uygulaması.

**Architecture:** ASP.NET Core 8 Web API, SNMP işlemlerini Lextm.SharpSnmpLib ile yapar; in-memory ConcurrentQueue ile geçmiş tutar. React + Vite + TypeScript frontend, polling ile her N saniyede veri çeker. Production'da React build çıktısı API'nin wwwroot'una kopyalanarak tek port üzerinden serve edilir.

**Tech Stack:** ASP.NET Core 8, Lextm.SharpSnmpLib 12.5.7, BCrypt.Net-Next, xUnit, Moq — React 18, TypeScript, Vite 5, Tailwind CSS 3, Recharts, Vitest

---

## Dosya Haritası

```
UpsPoC/
├── UpsPoC.sln
├── UpsPoC.Api/
│   ├── UpsPoC.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── UpsController.cs
│   ├── Services/
│   │   ├── ISnmpService.cs
│   │   ├── SnmpService.cs
│   │   ├── IUpsDataService.cs
│   │   └── UpsDataService.cs
│   └── Models/
│       ├── AppSettings.cs
│       ├── UpsStatus.cs
│       ├── UpsSnapshot.cs
│       ├── UpsConfig.cs
│       └── UpsCommand.cs
├── UpsPoC.Api.Tests/
│   ├── UpsPoC.Api.Tests.csproj
│   └── Services/
│       └── UpsDataServiceTests.cs
└── UpsPoC.Web/
    ├── package.json
    ├── tsconfig.json
    ├── vite.config.ts
    ├── tailwind.config.js
    ├── postcss.config.js
    ├── index.html
    └── src/
        ├── main.tsx
        ├── App.tsx
        ├── index.css
        ├── types/
        │   └── index.ts
        ├── api/
        │   └── client.ts
        ├── hooks/
        │   └── useUpsData.ts
        ├── components/
        │   ├── MetricCard.tsx
        │   ├── PowerChart.tsx
        │   ├── CommandPanel.tsx
        │   ├── ConfirmDialog.tsx
        │   ├── AlarmPanel.tsx
        │   ├── DeviceInfo.tsx
        │   └── ConfigModal.tsx
        └── pages/
            ├── Dashboard.tsx
            └── Login.tsx
```

---

## Task 1: Solution ve Proje İskeletleri

**Files:**
- Create: `UpsPoC.sln`
- Create: `UpsPoC.Api/UpsPoC.Api.csproj`
- Create: `UpsPoC.Api.Tests/UpsPoC.Api.Tests.csproj`

- [ ] **Step 1: Solution ve projeleri oluştur**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC

dotnet new sln -n UpsPoC
dotnet new webapi -n UpsPoC.Api --no-openapi -f net8.0
dotnet new xunit -n UpsPoC.Api.Tests -f net8.0
dotnet sln add UpsPoC.Api/UpsPoC.Api.csproj
dotnet sln add UpsPoC.Api.Tests/UpsPoC.Api.Tests.csproj
```

- [ ] **Step 2: NuGet paketlerini yükle (API projesi)**

```bash
cd UpsPoC.Api
dotnet add package Lextm.SharpSnmpLib --version 12.5.7
dotnet add package BCrypt.Net-Next --version 4.0.3
```

- [ ] **Step 3: NuGet paketlerini yükle (Test projesi)**

```bash
cd ../UpsPoC.Api.Tests
dotnet add reference ../UpsPoC.Api/UpsPoC.Api.csproj
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
```

- [ ] **Step 4: Varsayılan boilerplate dosyalarını temizle**

```bash
cd ../UpsPoC.Api
rm -f Controllers/WeatherForecastController.cs WeatherForecast.cs
cd ../UpsPoC.Api.Tests
rm -f UnitTest1.cs
```

- [ ] **Step 5: Frontend projesini oluştur**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
npm create vite@latest UpsPoC.Web -- --template react-ts
cd UpsPoC.Web
npm install
npm install recharts
npm install -D tailwindcss@3 postcss autoprefixer
npx tailwindcss init -p
```

- [ ] **Step 6: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git init
git add .
git commit -m "chore: initial solution and project scaffolding"
```

---

## Task 2: Models ve AppSettings

**Files:**
- Create: `UpsPoC.Api/Models/AppSettings.cs`
- Create: `UpsPoC.Api/Models/UpsStatus.cs`
- Create: `UpsPoC.Api/Models/UpsSnapshot.cs`
- Create: `UpsPoC.Api/Models/UpsConfig.cs`
- Create: `UpsPoC.Api/Models/UpsCommand.cs`
- Modify: `UpsPoC.Api/appsettings.json`

- [ ] **Step 1: AppSettings.cs oluştur**

```csharp
// UpsPoC.Api/Models/AppSettings.cs
namespace UpsPoC.Api.Models;

public class AppSettings
{
    public UpsSettings Ups { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
}

public class UpsSettings
{
    public string Host { get; set; } = "192.168.143.246";
    public int Port { get; set; } = 161;
    public string ReadCommunity { get; set; } = "public";
    public string WriteCommunity { get; set; } = "private";
    public int TimeoutMs { get; set; } = 3000;
    public int DefaultPollingIntervalSeconds { get; set; } = 5;
}

public class AuthSettings
{
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = string.Empty;
}
```

- [ ] **Step 2: UpsStatus.cs oluştur**

```csharp
// UpsPoC.Api/Models/UpsStatus.cs
namespace UpsPoC.Api.Models;

public class UpsStatus
{
    // Kimlik
    public string ModelName { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string AttachedDevices { get; set; } = string.Empty;

    // Batarya
    public int BatteryStatus { get; set; }          // 1=bilinmiyor 2=normal 3=düşük 4=kritik
    public string BatteryStatusText => BatteryStatus switch
    {
        2 => "Normal",
        3 => "Düşük",
        4 => "Kritik",
        _ => "Bilinmiyor"
    };
    public int BatteryRemainingMinutes { get; set; }
    public double BatteryVoltage { get; set; }      // 0.1V → V
    public int BatteryTemperature { get; set; }     // °C

    // Giriş
    public double InputVoltage { get; set; }        // VAC
    public double InputFrequency { get; set; }      // 0.1Hz → Hz

    // Çıkış
    public int OutputSource { get; set; }           // 1=diğer 2=normal 3=bypass 4=batarya 5=booster 6=reducer
    public string OutputSourceText => OutputSource switch
    {
        2 => "Normal",
        3 => "Bypass",
        4 => "Batarya",
        5 => "Booster",
        6 => "Reducer",
        _ => "Diğer"
    };
    public double OutputFrequency { get; set; }     // Hz
    public double OutputVoltage { get; set; }       // VAC
    public double OutputCurrent { get; set; }       // 0.1A → A
    public int OutputLoadPercent { get; set; }      // %
    public int OutputPowerWatts { get; set; }       // W

    // Alarm
    public int ActiveAlarmCount { get; set; }

    // Metadata
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsConnected { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
```

- [ ] **Step 3: UpsSnapshot.cs oluştur**

```csharp
// UpsPoC.Api/Models/UpsSnapshot.cs
namespace UpsPoC.Api.Models;

public class UpsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int BatteryPercent { get; set; }
    public int OutputLoadPercent { get; set; }
    public double InputVoltage { get; set; }
    public double OutputVoltage { get; set; }
    public int BatteryRemainingMinutes { get; set; }
    public int OutputPowerWatts { get; set; }
}
```

- [ ] **Step 4: UpsConfig.cs oluştur**

```csharp
// UpsPoC.Api/Models/UpsConfig.cs
namespace UpsPoC.Api.Models;

public class UpsConfig
{
    public int InputVoltageNominal { get; set; }     // RMS Volt
    public int InputFreqNominal { get; set; }        // 0.1 Hz (500 = 50Hz)
    public int OutputVoltageNominal { get; set; }    // RMS Volt
    public int OutputFreqNominal { get; set; }       // 0.1 Hz
    public int LowBatteryMinutes { get; set; }       // dakika
    public int AudibleStatus { get; set; }           // 1=kapalı 2=açık 3=geçici sessiz
    public int LowVoltageTransferPoint { get; set; } // RMS Volt
    public int HighVoltageTransferPoint { get; set; }// RMS Volt
}
```

- [ ] **Step 5: UpsCommand.cs oluştur**

```csharp
// UpsPoC.Api/Models/UpsCommand.cs
namespace UpsPoC.Api.Models;

public class UpsCommand
{
    public string CommandName { get; set; } = string.Empty;
    public int? IntValue { get; set; }
    public string? StringValue { get; set; }
}
```

- [ ] **Step 6: appsettings.json güncelle**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
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
    "PasswordHash": "$2a$11$placeholder.will.be.replaced.on.first.run"
  }
}
```

- [ ] **Step 7: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Models/ UpsPoC.Api/appsettings.json
git commit -m "feat: add domain models and app settings"
```

---

## Task 3: ISnmpService + SnmpService (GET)

**Files:**
- Create: `UpsPoC.Api/Services/ISnmpService.cs`
- Create: `UpsPoC.Api/Services/SnmpService.cs`

- [ ] **Step 1: ISnmpService interface oluştur**

```csharp
// UpsPoC.Api/Services/ISnmpService.cs
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public interface ISnmpService
{
    Task<UpsStatus> GetStatusAsync();
    Task<UpsConfig> GetConfigAsync();
    Task SetIntAsync(string oid, int value);
    Task SetStringAsync(string oid, string value);
    Task RunBatteryTestAsync();
}
```

- [ ] **Step 2: SnmpService.cs oluştur**

```csharp
// UpsPoC.Api/Services/SnmpService.cs
using System.Net;
using Lextudio.SharpSnmpLib;
using Lextudio.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Options;
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public class SnmpService : ISnmpService
{
    private readonly UpsSettings _settings;
    private readonly ILogger<SnmpService> _logger;

    // RFC 1628 Read OIDs
    private static class Oids
    {
        public const string ModelName             = "1.3.6.1.2.1.33.1.1.2.0";
        public const string FirmwareVersion       = "1.3.6.1.2.1.33.1.1.4.0";
        public const string AttachedDevices       = "1.3.6.1.2.1.33.1.1.6.0";

        public const string BatteryStatus         = "1.3.6.1.2.1.33.1.2.1.0";
        public const string BatteryRemainingMins  = "1.3.6.1.2.1.33.1.2.3.0";
        public const string BatteryVoltage        = "1.3.6.1.2.1.33.1.2.5.0";
        public const string BatteryTemperature    = "1.3.6.1.2.1.33.1.2.7.0";

        public const string InputFrequency        = "1.3.6.1.2.1.33.1.3.3.1.2.1";
        public const string InputVoltage          = "1.3.6.1.2.1.33.1.3.3.1.3.1";

        public const string OutputSource          = "1.3.6.1.2.1.33.1.4.1.0";
        public const string OutputFrequency       = "1.3.6.1.2.1.33.1.4.2.0";
        public const string OutputVoltage         = "1.3.6.1.2.1.33.1.4.4.1.2.1";
        public const string OutputCurrent         = "1.3.6.1.2.1.33.1.4.4.1.3.1";
        public const string OutputLoadPercent     = "1.3.6.1.2.1.33.1.4.4.1.5.1";
        public const string OutputPowerWatts      = "1.3.6.1.2.1.33.1.4.4.1.7.1";

        public const string ActiveAlarmCount      = "1.3.6.1.2.1.33.1.6.1.0";

        // Config OIDs
        public const string ConfigInputVoltage    = "1.3.6.1.2.1.33.1.9.1.0";
        public const string ConfigInputFreq       = "1.3.6.1.2.1.33.1.9.2.0";
        public const string ConfigOutputVoltage   = "1.3.6.1.2.1.33.1.9.3.0";
        public const string ConfigOutputFreq      = "1.3.6.1.2.1.33.1.9.4.0";
        public const string ConfigLowBattMins     = "1.3.6.1.2.1.33.1.9.7.0";
        public const string ConfigAudibleStatus   = "1.3.6.1.2.1.33.1.9.8.0";
        public const string ConfigLowVoltTrans    = "1.3.6.1.2.1.33.1.9.9.0";
        public const string ConfigHighVoltTrans   = "1.3.6.1.2.1.33.1.9.10.0";

        // Test OIDs
        public const string TestId                = "1.3.6.1.2.1.33.1.7.1.0";
        public const string TestSpinLock          = "1.3.6.1.2.1.33.1.7.2.0";
    }

    public SnmpService(IOptions<AppSettings> options, ILogger<SnmpService> logger)
    {
        _settings = options.Value.Ups;
        _logger = logger;
    }

    private IPEndPoint Endpoint => new(IPAddress.Parse(_settings.Host), _settings.Port);
    private OctetString ReadCommunity => new(_settings.ReadCommunity);
    private OctetString WriteCommunity => new(_settings.WriteCommunity);

    private async Task<IList<Variable>> GetAsync(params string[] oids)
    {
        var variables = oids.Select(o => new Variable(new ObjectIdentifier(o))).ToList();
        return await Messenger.GetAsync(
            VersionCode.V2,
            Endpoint,
            ReadCommunity,
            variables);
    }

    private int GetInt(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        return data is NoSuchInstance or NoSuchObject ? 0 : int.Parse(data.ToString()!);
    }

    private string GetStr(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        return data is NoSuchInstance or NoSuchObject ? string.Empty : data.ToString()!;
    }

    public async Task<UpsStatus> GetStatusAsync()
    {
        try
        {
            var vars = await GetAsync(
                Oids.ModelName, Oids.FirmwareVersion, Oids.AttachedDevices,
                Oids.BatteryStatus, Oids.BatteryRemainingMins, Oids.BatteryVoltage, Oids.BatteryTemperature,
                Oids.InputFrequency, Oids.InputVoltage,
                Oids.OutputSource, Oids.OutputFrequency, Oids.OutputVoltage,
                Oids.OutputCurrent, Oids.OutputLoadPercent, Oids.OutputPowerWatts,
                Oids.ActiveAlarmCount);

            return new UpsStatus
            {
                ModelName             = GetStr(vars, 0),
                FirmwareVersion       = GetStr(vars, 1),
                AttachedDevices       = GetStr(vars, 2),
                BatteryStatus         = GetInt(vars, 3),
                BatteryRemainingMinutes = GetInt(vars, 4),
                BatteryVoltage        = GetInt(vars, 5) / 10.0,
                BatteryTemperature    = GetInt(vars, 6),
                InputFrequency        = GetInt(vars, 7) / 10.0,
                InputVoltage          = GetInt(vars, 8),
                OutputSource          = GetInt(vars, 9),
                OutputFrequency       = GetInt(vars, 10) / 10.0,
                OutputVoltage         = GetInt(vars, 11),
                OutputCurrent         = GetInt(vars, 12) / 10.0,
                OutputLoadPercent     = GetInt(vars, 13),
                OutputPowerWatts      = GetInt(vars, 14),
                ActiveAlarmCount      = GetInt(vars, 15),
                Timestamp             = DateTime.UtcNow,
                IsConnected           = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SNMP GET status failed");
            return new UpsStatus { IsConnected = false, ErrorMessage = ex.Message, Timestamp = DateTime.UtcNow };
        }
    }

    public async Task<UpsConfig> GetConfigAsync()
    {
        try
        {
            var vars = await GetAsync(
                Oids.ConfigInputVoltage, Oids.ConfigInputFreq,
                Oids.ConfigOutputVoltage, Oids.ConfigOutputFreq,
                Oids.ConfigLowBattMins, Oids.ConfigAudibleStatus,
                Oids.ConfigLowVoltTrans, Oids.ConfigHighVoltTrans);

            return new UpsConfig
            {
                InputVoltageNominal      = GetInt(vars, 0),
                InputFreqNominal         = GetInt(vars, 1),
                OutputVoltageNominal     = GetInt(vars, 2),
                OutputFreqNominal        = GetInt(vars, 3),
                LowBatteryMinutes        = GetInt(vars, 4),
                AudibleStatus            = GetInt(vars, 5),
                LowVoltageTransferPoint  = GetInt(vars, 6),
                HighVoltageTransferPoint = GetInt(vars, 7)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SNMP GET config failed");
            throw;
        }
    }

    public async Task SetIntAsync(string oid, int value)
    {
        var variables = new List<Variable>
        {
            new(new ObjectIdentifier(oid), new Integer32(value))
        };
        await Messenger.SetAsync(
            VersionCode.V2,
            Endpoint,
            WriteCommunity,
            variables);
    }

    public async Task SetStringAsync(string oid, string value)
    {
        var variables = new List<Variable>
        {
            new(new ObjectIdentifier(oid), new OctetString(value))
        };
        await Messenger.SetAsync(
            VersionCode.V2,
            Endpoint,
            WriteCommunity,
            variables);
    }

    public async Task RunBatteryTestAsync()
    {
        // upsTestSpinLock SET ile test başlatılır
        await SetIntAsync(Oids.TestSpinLock, 1);
    }
}
```

- [ ] **Step 3: Build al, derleme hatası yoksa devam et**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Services/
git commit -m "feat: add SNMP service with RFC 1628 GET/SET support"
```

---

## Task 4: IUpsDataService + UpsDataService (In-Memory Geçmiş)

**Files:**
- Create: `UpsPoC.Api/Services/IUpsDataService.cs`
- Create: `UpsPoC.Api/Services/UpsDataService.cs`
- Create: `UpsPoC.Api.Tests/Services/UpsDataServiceTests.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
// UpsPoC.Api.Tests/Services/UpsDataServiceTests.cs
using FluentAssertions;
using UpsPoC.Api.Models;
using UpsPoC.Api.Services;

namespace UpsPoC.Api.Tests.Services;

public class UpsDataServiceTests
{
    [Fact]
    public void GetLatestStatus_WhenNoDataYet_ReturnsDisconnectedStatus()
    {
        var service = new UpsDataService();
        var result = service.GetLatestStatus();
        result.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void GetHistory_WhenEmpty_ReturnsEmptyList()
    {
        var service = new UpsDataService();
        var result = service.GetHistory();
        result.Should().BeEmpty();
    }

    [Fact]
    public void AddSnapshot_ThenGetHistory_ReturnsThatSnapshot()
    {
        var service = new UpsDataService();
        var status = new UpsStatus
        {
            BatteryStatus = 2,
            BatteryRemainingMinutes = 42,
            OutputLoadPercent = 35,
            InputVoltage = 220,
            OutputVoltage = 220,
            OutputPowerWatts = 210,
            IsConnected = true,
            Timestamp = DateTime.UtcNow
        };

        service.UpdateStatus(status);

        var history = service.GetHistory();
        history.Should().HaveCount(1);
        history[0].OutputLoadPercent.Should().Be(35);
        history[0].InputVoltage.Should().Be(220);
    }

    [Fact]
    public void UpdateStatus_ExceedsMaxSnapshots_OldestIsDropped()
    {
        var service = new UpsDataService();

        for (int i = 0; i < 721; i++)
        {
            service.UpdateStatus(new UpsStatus
            {
                OutputLoadPercent = i,
                IsConnected = true,
                Timestamp = DateTime.UtcNow
            });
        }

        var history = service.GetHistory();
        history.Should().HaveCount(720);
        history[0].OutputLoadPercent.Should().Be(1); // 0 dropped
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api.Tests
dotnet test --filter "UpsDataServiceTests"
```

Expected: Derleme hatası — `UpsDataService` henüz yok.

- [ ] **Step 3: IUpsDataService interface oluştur**

```csharp
// UpsPoC.Api/Services/IUpsDataService.cs
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public interface IUpsDataService
{
    UpsStatus GetLatestStatus();
    List<UpsSnapshot> GetHistory();
    void UpdateStatus(UpsStatus status);
}
```

- [ ] **Step 4: UpsDataService implement et**

```csharp
// UpsPoC.Api/Services/UpsDataService.cs
using System.Collections.Concurrent;
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public class UpsDataService : IUpsDataService
{
    private const int MaxSnapshots = 720;

    private readonly ConcurrentQueue<UpsSnapshot> _history = new();
    private UpsStatus _latestStatus = new() { IsConnected = false };

    public UpsStatus GetLatestStatus() => _latestStatus;

    public List<UpsSnapshot> GetHistory() => _history.ToList();

    public void UpdateStatus(UpsStatus status)
    {
        _latestStatus = status;

        if (!status.IsConnected) return;

        _history.Enqueue(new UpsSnapshot
        {
            Timestamp             = status.Timestamp,
            BatteryPercent        = status.BatteryStatus == 2 ? 100 :
                                    status.BatteryStatus == 3 ? 30 :
                                    status.BatteryStatus == 4 ? 5 : 0,
            OutputLoadPercent     = status.OutputLoadPercent,
            InputVoltage          = status.InputVoltage,
            OutputVoltage         = status.OutputVoltage,
            BatteryRemainingMinutes = status.BatteryRemainingMinutes,
            OutputPowerWatts      = status.OutputPowerWatts
        });

        while (_history.Count > MaxSnapshots)
            _history.TryDequeue(out _);
    }
}
```

- [ ] **Step 5: Testleri çalıştır, geçtiğini doğrula**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api.Tests
dotnet test --filter "UpsDataServiceTests"
```

Expected: `Passed! - 4 tests`

- [ ] **Step 6: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Services/IUpsDataService.cs UpsPoC.Api/Services/UpsDataService.cs UpsPoC.Api.Tests/
git commit -m "feat: add UpsDataService with in-memory history (max 720 snapshots)"
```

---

## Task 5: UpsController (Read Endpoints)

**Files:**
- Create: `UpsPoC.Api/Controllers/UpsController.cs`

- [ ] **Step 1: UpsController oluştur**

```csharp
// UpsPoC.Api/Controllers/UpsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsPoC.Api.Models;
using UpsPoC.Api.Services;

namespace UpsPoC.Api.Controllers;

[ApiController]
[Route("api/ups")]
[Authorize]
public class UpsController : ControllerBase
{
    private readonly IUpsDataService _upsData;
    private readonly ISnmpService _snmp;

    public UpsController(IUpsDataService upsData, ISnmpService snmp)
    {
        _upsData = upsData;
        _snmp = snmp;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        // Her GET isteğinde SNMP'den taze veri çek, servisi güncelle
        var status = await _snmp.GetStatusAsync();
        _upsData.UpdateStatus(status);
        return Ok(status);
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        return Ok(_upsData.GetHistory());
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        try
        {
            var config = await _snmp.GetConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = ex.Message });
        }
    }
}
```

- [ ] **Step 2: Build kontrol**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Controllers/UpsController.cs
git commit -m "feat: add UPS read endpoints (status, history, config)"
```

---

## Task 6: Komut Endpoint'leri (SNMP SET)

**Files:**
- Modify: `UpsPoC.Api/Controllers/UpsController.cs`

- [ ] **Step 1: UpsController'a command ve config endpoint'leri ekle**

`UpsController.cs` dosyasını aç, sınıfın sonuna şu metodları ekle (son `}` parantezinden önce):

```csharp
    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] UpsCommand command)
    {
        try
        {
            switch (command.CommandName)
            {
                // Tehlikeli komutlar — onay frontend'de sağlanmış olmalı
                case "shutdown-type":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.1.0", command.IntValue ?? 1);
                    break;
                case "shutdown-after-delay":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.2.0", command.IntValue ?? 60);
                    break;
                case "startup-after-delay":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.3.0", command.IntValue ?? 60);
                    break;
                case "reboot":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.4.0", command.IntValue ?? 60);
                    break;
                case "abort-shutdown":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.2.0", -1);
                    break;
                case "abort-startup":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.3.0", -1);
                    break;

                // Normal komutlar
                case "auto-restart":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.5.0", command.IntValue ?? 1);
                    break;
                case "battery-test":
                    await _snmp.RunBatteryTestAsync();
                    break;
                case "audible-alarm":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.8.0", command.IntValue ?? 2);
                    break;
                case "set-name":
                    if (!string.IsNullOrWhiteSpace(command.StringValue))
                        await _snmp.SetStringAsync("1.3.6.1.2.1.33.1.1.5.0", command.StringValue[..Math.Min(63, command.StringValue.Length)]);
                    break;

                default:
                    return BadRequest(new { error = $"Bilinmeyen komut: {command.CommandName}" });
            }

            return Ok(new { success = true, command = command.CommandName });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = $"Komut gönderilemedi: {ex.Message}" });
        }
    }

    [HttpPost("config")]
    public async Task<IActionResult> SetConfig([FromBody] UpsConfig config)
    {
        try
        {
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.1.0", config.InputVoltageNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.2.0", config.InputFreqNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.3.0", config.OutputVoltageNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.4.0", config.OutputFreqNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.7.0", config.LowBatteryMinutes);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.8.0", config.AudibleStatus);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.9.0", config.LowVoltageTransferPoint);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.10.0", config.HighVoltageTransferPoint);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = $"Konfigürasyon kaydedilemedi: {ex.Message}" });
        }
    }
```

- [ ] **Step 2: Build kontrol**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Controllers/UpsController.cs
git commit -m "feat: add command and config SET endpoints"
```

---

## Task 7: Auth Sistemi

**Files:**
- Create: `UpsPoC.Api/Controllers/AuthController.cs`

- [ ] **Step 1: AuthController oluştur**

```csharp
// UpsPoC.Api/Controllers/AuthController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthSettings _auth;

    public AuthController(IOptions<AppSettings> options)
    {
        _auth = options.Value.Auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request.Username != _auth.Username)
            return Unauthorized(new { error = "Kullanıcı adı veya şifre hatalı." });

        bool passwordOk;
        try
        {
            passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, _auth.PasswordHash);
        }
        catch
        {
            passwordOk = false;
        }

        if (!passwordOk)
            return Unauthorized(new { error = "Kullanıcı adı veya şifre hatalı." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

        return Ok(new { username = request.Username });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { success = true });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new { username = User.Identity?.Name });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Build kontrol**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Controllers/AuthController.cs
git commit -m "feat: add cookie-based auth (login/logout/me)"
```

---

## Task 8: Program.cs — Tüm Servisleri Bağla

**Files:**
- Modify: `UpsPoC.Api/Program.cs`

- [ ] **Step 1: Program.cs'i yeniden yaz**

```csharp
// UpsPoC.Api/Program.cs
using Microsoft.AspNetCore.Authentication.Cookies;
using UpsPoC.Api.Models;
using UpsPoC.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<AppSettings>(builder.Configuration);

// SNMP + Data services
builder.Services.AddSingleton<ISnmpService, SnmpService>();
builder.Services.AddSingleton<IUpsDataService, UpsDataService>(_ => new UpsDataService());

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// CORS — dev modda Vite dev server'a izin ver
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("DevCors");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SPA fallback — production'da React routing için
app.MapFallbackToFile("index.html");

app.Run();
```

- [ ] **Step 2: Parolanın BCrypt hash'ini oluştur**

Terminalde tek seferlik çalıştır:

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet script -e "Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(\"admin123\"));"
```

Eğer `dotnet script` yoksa alternatif — küçük bir C# scripti yaz:

```bash
cat > /tmp/hash.csx << 'EOF'
#r "nuget: BCrypt.Net-Next, 4.0.3"
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(args[0]));
EOF
dotnet script /tmp/hash.csx -- "admin123"
```

Ya da basitçe: `appsettings.json`'daki `PasswordHash` değerini aşağıdaki ile değiştir (bu hash `admin123` şifresinin BCrypt karşılığıdır):

```
$2a$11$rOzJqYmZgKQxcBnGdN5zOOt.5QFQfJ2xXmqxfNOSJQTZ6DpIV2YSa
```

Ardından `appsettings.json` içindeki `PasswordHash` alanını bu değerle güncelle.

**Not:** Production'da bu şifreyi değiştirin.

- [ ] **Step 3: API'yi başlat ve sağlık kontrolü yap**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet run --urls "http://0.0.0.0:5000"
```

Yeni terminalde:

```bash
# Login ol
curl -c cookies.txt -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Me endpoint
curl -b cookies.txt http://localhost:5000/api/auth/me
```

Expected: `{"username":"admin"}`

- [ ] **Step 4: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Api/Program.cs UpsPoC.Api/appsettings.json
git commit -m "feat: wire all services, auth, CORS in Program.cs"
```

---

## Task 9: Frontend Yapılandırma (Vite + Tailwind + TypeScript)

**Files:**
- Modify: `UpsPoC.Web/vite.config.ts`
- Modify: `UpsPoC.Web/tailwind.config.js`
- Create: `UpsPoC.Web/src/index.css`
- Modify: `UpsPoC.Web/index.html`
- Create: `UpsPoC.Web/src/types/index.ts`

- [ ] **Step 1: vite.config.ts güncelle (API proxy)**

```typescript
// UpsPoC.Web/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
```

- [ ] **Step 2: tailwind.config.js güncelle**

```javascript
// UpsPoC.Web/tailwind.config.js
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        dark: {
          900: '#0f172a',
          800: '#1e293b',
          700: '#334155',
        }
      }
    },
  },
  plugins: [],
}
```

- [ ] **Step 3: src/index.css oluştur**

```css
/* UpsPoC.Web/src/index.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

body {
  @apply bg-slate-900 text-slate-100;
  font-family: 'Inter', system-ui, sans-serif;
}

::-webkit-scrollbar {
  width: 6px;
}
::-webkit-scrollbar-track {
  @apply bg-slate-800;
}
::-webkit-scrollbar-thumb {
  @apply bg-slate-600 rounded;
}
```

- [ ] **Step 4: src/types/index.ts oluştur**

```typescript
// UpsPoC.Web/src/types/index.ts

export interface UpsStatus {
  modelName: string;
  firmwareVersion: string;
  attachedDevices: string;
  batteryStatus: number;
  batteryStatusText: string;
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
```

- [ ] **Step 5: main.tsx güncelle**

```typescript
// UpsPoC.Web/src/main.tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
```

- [ ] **Step 6: Frontend build kontrol**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Web
npm run build
```

Expected: `✓ built in ...`

- [ ] **Step 7: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/
git commit -m "feat: configure Vite, Tailwind, TypeScript types"
```

---

## Task 10: API Client + useUpsData Hook

**Files:**
- Create: `UpsPoC.Web/src/api/client.ts`
- Create: `UpsPoC.Web/src/hooks/useUpsData.ts`

- [ ] **Step 1: API client oluştur**

```typescript
// UpsPoC.Web/src/api/client.ts
import type { UpsCommand, UpsConfig } from '../types';

const BASE = '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (res.status === 401) {
    window.location.href = '/login';
    throw new Error('Oturum süresi doldu');
  }

  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(body.error ?? res.statusText);
  }

  return res.json();
}

export const api = {
  auth: {
    login: (username: string, password: string) =>
      request('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
      }),
    logout: () => request('/auth/logout', { method: 'POST' }),
    me: () => request<{ username: string }>('/auth/me'),
  },
  ups: {
    getStatus: () => request('/ups/status'),
    getHistory: () => request('/ups/history'),
    getConfig: () => request<UpsConfig>('/ups/config'),
    sendCommand: (cmd: UpsCommand) =>
      request('/ups/command', { method: 'POST', body: JSON.stringify(cmd) }),
    setConfig: (config: UpsConfig) =>
      request('/ups/config', { method: 'POST', body: JSON.stringify(config) }),
  },
};
```

- [ ] **Step 2: useUpsData hook oluştur**

```typescript
// UpsPoC.Web/src/hooks/useUpsData.ts
import { useState, useEffect, useCallback, useRef } from 'react';
import { api } from '../api/client';
import type { UpsStatus, UpsSnapshot } from '../types';

export function useUpsData(intervalSeconds: number) {
  const [status, setStatus] = useState<UpsStatus | null>(null);
  const [history, setHistory] = useState<UpsSnapshot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchData = useCallback(async () => {
    try {
      const [newStatus, newHistory] = await Promise.all([
        api.ups.getStatus() as Promise<UpsStatus>,
        api.ups.getHistory() as Promise<UpsSnapshot[]>,
      ]);
      setStatus(newStatus);
      setHistory(newHistory);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bağlantı hatası');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
    intervalRef.current = setInterval(fetchData, intervalSeconds * 1000);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [fetchData, intervalSeconds]);

  return { status, history, isLoading, error, refetch: fetchData };
}
```

- [ ] **Step 3: Build kontrol**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Web
npm run build
```

Expected: `✓ built in ...`

- [ ] **Step 4: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/api/ UpsPoC.Web/src/hooks/
git commit -m "feat: add API client and useUpsData polling hook"
```

---

## Task 11: Login Sayfası

**Files:**
- Create: `UpsPoC.Web/src/pages/Login.tsx`

- [ ] **Step 1: Login.tsx oluştur**

```tsx
// UpsPoC.Web/src/pages/Login.tsx
import { useState, FormEvent } from 'react';
import { api } from '../api/client';

interface Props {
  onLogin: () => void;
}

export default function Login({ onLogin }: Props) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await api.auth.login(username, password);
      onLogin();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Giriş başarısız');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-900 flex items-center justify-center">
      <div className="bg-slate-800 rounded-xl p-8 w-full max-w-sm border border-slate-700">
        <div className="text-center mb-8">
          <div className="text-4xl mb-2">⚡</div>
          <h1 className="text-xl font-bold text-slate-100">UPS Monitor</h1>
          <p className="text-slate-400 text-sm mt-1">EA900 · 192.168.143.246</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Kullanıcı Adı</label>
            <input
              type="text"
              value={username}
              onChange={e => setUsername(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-3 py-2 text-slate-100 focus:outline-none focus:border-sky-500"
              required
              autoFocus
            />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Şifre</label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-3 py-2 text-slate-100 focus:outline-none focus:border-sky-500"
              required
            />
          </div>

          {error && (
            <p className="text-red-400 text-sm bg-red-900/20 border border-red-800 rounded p-2">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-sky-600 hover:bg-sky-500 disabled:opacity-50 text-white font-medium py-2 px-4 rounded-lg transition-colors"
          >
            {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
          </button>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/pages/Login.tsx
git commit -m "feat: add login page"
```

---

## Task 12: MetricCard Bileşeni

**Files:**
- Create: `UpsPoC.Web/src/components/MetricCard.tsx`

- [ ] **Step 1: MetricCard.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/MetricCard.tsx
interface Props {
  label: string;
  value: string;
  subValue?: string;
  color: 'green' | 'blue' | 'yellow' | 'purple' | 'orange';
  progress?: number; // 0-100
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
```

- [ ] **Step 2: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/components/MetricCard.tsx
git commit -m "feat: add MetricCard component"
```

---

## Task 13: PowerChart Bileşeni (Recharts)

**Files:**
- Create: `UpsPoC.Web/src/components/PowerChart.tsx`

- [ ] **Step 1: PowerChart.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/PowerChart.tsx
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
      <div className="bg-slate-800 rounded-xl p-4 border border-slate-700 flex items-center justify-center h-40">
        <p className="text-slate-500 text-sm">Geçmiş veri toplanıyor...</p>
      </div>
    );
  }

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Güç Geçmişi</h3>
      <ResponsiveContainer width="100%" height={160}>
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
```

- [ ] **Step 2: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/components/PowerChart.tsx
git commit -m "feat: add PowerChart component with Recharts"
```

---

## Task 14: ConfirmDialog + CommandPanel Bileşenleri

**Files:**
- Create: `UpsPoC.Web/src/components/ConfirmDialog.tsx`
- Create: `UpsPoC.Web/src/components/CommandPanel.tsx`

- [ ] **Step 1: ConfirmDialog.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/ConfirmDialog.tsx
interface Props {
  title: string;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmDialog({ title, message, onConfirm, onCancel }: Props) {
  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-slate-800 border border-slate-600 rounded-xl p-6 max-w-sm w-full mx-4">
        <h3 className="text-red-400 font-semibold text-lg mb-2">{title}</h3>
        <p className="text-slate-300 text-sm mb-6">{message}</p>
        <div className="flex gap-3 justify-end">
          <button
            onClick={onCancel}
            className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded-lg text-sm transition-colors"
          >
            İptal
          </button>
          <button
            onClick={onConfirm}
            className="px-4 py-2 bg-red-600 hover:bg-red-500 rounded-lg text-sm font-medium transition-colors"
          >
            Onayla
          </button>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: CommandPanel.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/CommandPanel.tsx
import { useState } from 'react';
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

  const showToast = (message: string, type: 'success' | 'error') => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

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

      {/* Güç Kontrolü */}
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
            min={0} max={300}
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
          ✕ Kapatmayı İptal Et (-1)
        </button>
      </div>

      {/* Test & Ayar */}
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

      {/* Confirm Dialog */}
      {pending && (
        <ConfirmDialog
          title={pending.title}
          message={pending.message}
          onConfirm={() => { executeCommand(pending.command); setPending(null); }}
          onCancel={() => setPending(null)}
        />
      )}

      {/* Toast */}
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
```

- [ ] **Step 3: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/components/ConfirmDialog.tsx UpsPoC.Web/src/components/CommandPanel.tsx
git commit -m "feat: add CommandPanel with confirm dialogs and toast notifications"
```

---

## Task 15: AlarmPanel + DeviceInfo + ConfigModal

**Files:**
- Create: `UpsPoC.Web/src/components/AlarmPanel.tsx`
- Create: `UpsPoC.Web/src/components/DeviceInfo.tsx`
- Create: `UpsPoC.Web/src/components/ConfigModal.tsx`

- [ ] **Step 1: AlarmPanel.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/AlarmPanel.tsx
import type { UpsStatus } from '../types';

interface Props {
  status: UpsStatus | null;
}

export default function AlarmPanel({ status }: Props) {
  const alarmCount = status?.activeAlarmCount ?? 0;
  const source = status?.outputSourceText ?? '—';
  const isOnBattery = status?.outputSource === 4;

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Alarmlar & Durum</h3>
      <div className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Aktif Alarm</span>
          <span className={`text-sm font-semibold ${alarmCount > 0 ? 'text-red-400' : 'text-emerald-400'}`}>
            {alarmCount > 0 ? `⚠ ${alarmCount} alarm` : '✓ Alarm yok'}
          </span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Güç Kaynağı</span>
          <span className={`text-sm font-semibold ${isOnBattery ? 'text-amber-400' : 'text-emerald-400'}`}>
            {isOnBattery ? '⚡ ' : ''}{source}
          </span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-xs">Batarya Durumu</span>
          <span className={`text-sm font-semibold ${
            status?.batteryStatus === 2 ? 'text-emerald-400' :
            status?.batteryStatus === 3 ? 'text-amber-400' :
            status?.batteryStatus === 4 ? 'text-red-400' : 'text-slate-400'
          }`}>
            {status?.batteryStatusText ?? '—'}
          </span>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: DeviceInfo.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/DeviceInfo.tsx
import type { UpsStatus } from '../types';

interface Props {
  status: UpsStatus | null;
}

export default function DeviceInfo({ status }: Props) {
  const rows: [string, string][] = [
    ['Model', status?.modelName || '—'],
    ['Firmware', status?.firmwareVersion || '—'],
    ['Çıkış Voltajı', status ? `${status.outputVoltage} VAC` : '—'],
    ['Çıkış Frekansı', status ? `${status.outputFrequency} Hz` : '—'],
    ['Bağlı Cihazlar', status?.attachedDevices || '—'],
  ];

  return (
    <div className="bg-slate-800 rounded-xl p-4 border border-slate-700">
      <h3 className="text-slate-200 text-sm font-semibold mb-3">Cihaz Bilgisi</h3>
      <dl className="space-y-1.5">
        {rows.map(([label, value]) => (
          <div key={label} className="flex justify-between items-center">
            <dt className="text-slate-400 text-xs">{label}</dt>
            <dd className="text-slate-200 text-xs font-medium text-right max-w-[60%] truncate">{value}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
```

- [ ] **Step 3: ConfigModal.tsx oluştur**

```tsx
// UpsPoC.Web/src/components/ConfigModal.tsx
import { useState, useEffect } from 'react';
import { api } from '../api/client';
import type { UpsConfig } from '../types';

interface Props {
  onClose: () => void;
}

export default function ConfigModal({ onClose }: Props) {
  const [config, setConfig] = useState<UpsConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    api.ups.getConfig()
      .then(setConfig)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    if (!config) return;
    setSaving(true);
    setError('');
    try {
      await api.ups.setConfig(config);
      setSuccess(true);
      setTimeout(() => { setSuccess(false); onClose(); }, 1200);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Kaydedilemedi');
    } finally {
      setSaving(false);
    }
  };

  const update = (key: keyof UpsConfig, val: number) =>
    setConfig(prev => prev ? { ...prev, [key]: val } : prev);

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-slate-800 border border-slate-600 rounded-xl p-6 max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-slate-100 font-semibold">UPS Konfigürasyonu</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-200 text-xl">✕</button>
        </div>

        {loading && <p className="text-slate-400 text-sm">Yükleniyor...</p>}
        {error && <p className="text-red-400 text-sm bg-red-900/20 border border-red-800 rounded p-2 mb-3">{error}</p>}
        {success && <p className="text-emerald-400 text-sm">✓ Kaydedildi</p>}

        {config && (
          <div className="space-y-3">
            {([
              ['Nominal Giriş Voltajı (V)', 'inputVoltageNominal'],
              ['Nominal Giriş Frekansı (0.1Hz, örn. 500=50Hz)', 'inputFreqNominal'],
              ['Nominal Çıkış Voltajı (V)', 'outputVoltageNominal'],
              ['Nominal Çıkış Frekansı (0.1Hz)', 'outputFreqNominal'],
              ['Düşük Batarya Eşiği (dakika)', 'lowBatteryMinutes'],
              ['Düşük Voltaj Transfer Noktası (V)', 'lowVoltageTransferPoint'],
              ['Yüksek Voltaj Transfer Noktası (V)', 'highVoltageTransferPoint'],
            ] as [string, keyof UpsConfig][]).map(([label, key]) => (
              <div key={key}>
                <label className="block text-xs text-slate-400 mb-1">{label}</label>
                <input
                  type="number"
                  value={config[key] as number}
                  onChange={e => update(key, Number(e.target.value))}
                  className="w-full bg-slate-700 border border-slate-600 rounded px-3 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-sky-500"
                />
              </div>
            ))}

            <div>
              <label className="block text-xs text-slate-400 mb-1">Sesli Alarm</label>
              <select
                value={config.audibleStatus}
                onChange={e => update('audibleStatus', Number(e.target.value))}
                className="w-full bg-slate-700 border border-slate-600 rounded px-3 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-sky-500"
              >
                <option value={1}>Kapalı</option>
                <option value={2}>Açık</option>
                <option value={3}>Geçici Sessiz</option>
              </select>
            </div>

            <div className="flex gap-3 justify-end pt-2">
              <button onClick={onClose} className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded-lg text-sm transition-colors">
                İptal
              </button>
              <button
                onClick={handleSave}
                disabled={saving}
                className="px-4 py-2 bg-sky-600 hover:bg-sky-500 disabled:opacity-50 rounded-lg text-sm font-medium transition-colors"
              >
                {saving ? 'Kaydediliyor...' : 'Kaydet'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/components/AlarmPanel.tsx UpsPoC.Web/src/components/DeviceInfo.tsx UpsPoC.Web/src/components/ConfigModal.tsx
git commit -m "feat: add AlarmPanel, DeviceInfo, ConfigModal components"
```

---

## Task 16: Dashboard Sayfası

**Files:**
- Create: `UpsPoC.Web/src/pages/Dashboard.tsx`

- [ ] **Step 1: Dashboard.tsx oluştur**

```tsx
// UpsPoC.Web/src/pages/Dashboard.tsx
import { useState } from 'react';
import { api } from '../api/client';
import { useUpsData } from '../hooks/useUpsData';
import MetricCard from '../components/MetricCard';
import PowerChart from '../components/PowerChart';
import CommandPanel from '../components/CommandPanel';
import AlarmPanel from '../components/AlarmPanel';
import DeviceInfo from '../components/DeviceInfo';
import ConfigModal from '../components/ConfigModal';

interface Props {
  onLogout: () => void;
}

const INTERVAL_OPTIONS = [5, 10, 15, 30, 60];

export default function Dashboard({ onLogout }: Props) {
  const [interval, setIntervalSec] = useState(5);
  const [showConfig, setShowConfig] = useState(false);
  const { status, history, isLoading, error } = useUpsData(interval);

  const handleLogout = async () => {
    await api.auth.logout().catch(() => {});
    onLogout();
  };

  const dimmed = !status?.isConnected;

  const batteryProgress = status?.batteryStatus === 2 ? 98 :
                          status?.batteryStatus === 3 ? 30 :
                          status?.batteryStatus === 4 ? 5 : 0;

  return (
    <div className="min-h-screen bg-slate-900 p-4">
      {/* Top Bar */}
      <div className="flex justify-between items-center mb-5 pb-4 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <span className="text-sky-400 text-xl font-bold">⚡ UPS Monitor</span>
          <span className="text-slate-500 text-sm">EA900 · 192.168.143.246</span>
        </div>
        <div className="flex items-center gap-4">
          {isLoading && <span className="text-slate-500 text-xs animate-pulse">Bağlanıyor...</span>}
          {!isLoading && status?.isConnected && (
            <span className="bg-emerald-950 text-emerald-400 border border-emerald-800 rounded-full px-3 py-0.5 text-xs">● ONLİNE</span>
          )}
          {!isLoading && !status?.isConnected && (
            <span className="bg-red-950 text-red-400 border border-red-800 rounded-full px-3 py-0.5 text-xs">● BAĞLANTI YOK</span>
          )}
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span>Yenileme:</span>
            <select
              value={interval}
              onChange={e => setIntervalSec(Number(e.target.value))}
              className="bg-slate-800 border border-slate-700 rounded px-2 py-0.5 text-slate-300 focus:outline-none"
            >
              {INTERVAL_OPTIONS.map(s => (
                <option key={s} value={s}>{s}s</option>
              ))}
            </select>
          </div>
          <button
            onClick={() => setShowConfig(true)}
            className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
          >
            ⚙ Ayarlar
          </button>
          <button
            onClick={handleLogout}
            className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
          >
            🔓 Çıkış
          </button>
        </div>
      </div>

      {/* Connection error banner */}
      {error && (
        <div className="bg-red-900/30 border border-red-800 text-red-300 text-sm rounded-lg px-4 py-2 mb-4">
          ⚠ {error}
        </div>
      )}

      {/* Metric Cards */}
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
          value={status ? `${status.batteryRemainingMinutes}dk` : '—'}
          subValue={status ? `${status.batteryVoltage.toFixed(1)}V DC` : undefined}
          color="blue"
          progress={status ? Math.min(100, status.batteryRemainingMinutes / 60 * 100) : 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Yük"
          value={status ? `${status.outputLoadPercent}%` : '—'}
          subValue={status ? `${status.outputPowerWatts}W` : undefined}
          color="yellow"
          progress={status?.outputLoadPercent}
          dimmed={dimmed}
        />
        <MetricCard
          label="Giriş"
          value={status ? `${status.inputVoltage}V` : '—'}
          subValue={status ? `${status.inputFrequency.toFixed(1)} Hz` : undefined}
          color="purple"
          progress={status ? Math.min(100, (status.inputVoltage / 250) * 100) : 0}
          dimmed={dimmed}
        />
        <MetricCard
          label="Sıcaklık"
          value={status ? `${status.batteryTemperature}°C` : '—'}
          subValue="Batarya"
          color="orange"
          progress={status ? Math.min(100, status.batteryTemperature / 60 * 100) : 0}
          dimmed={dimmed}
        />
      </div>

      {/* Chart + Commands */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        <div className="lg:col-span-2">
          <PowerChart history={history} />
        </div>
        <CommandPanel />
      </div>

      {/* Alarm + Device Info */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <AlarmPanel status={status} />
        <DeviceInfo status={status} />
      </div>

      {showConfig && <ConfigModal onClose={() => setShowConfig(false)} />}
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/pages/Dashboard.tsx
git commit -m "feat: add Dashboard page with all components assembled"
```

---

## Task 17: App.tsx — Routing ve Auth Guard

**Files:**
- Modify: `UpsPoC.Web/src/App.tsx`

- [ ] **Step 1: App.tsx yeniden yaz**

```tsx
// UpsPoC.Web/src/App.tsx
import { useState, useEffect } from 'react';
import { api } from './api/client';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';

type AuthState = 'loading' | 'authenticated' | 'unauthenticated';

export default function App() {
  const [authState, setAuthState] = useState<AuthState>('loading');

  useEffect(() => {
    api.auth.me()
      .then(() => setAuthState('authenticated'))
      .catch(() => setAuthState('unauthenticated'));
  }, []);

  if (authState === 'loading') {
    return (
      <div className="min-h-screen bg-slate-900 flex items-center justify-center">
        <div className="text-slate-400 animate-pulse">Yükleniyor...</div>
      </div>
    );
  }

  if (authState === 'unauthenticated') {
    return <Login onLogin={() => setAuthState('authenticated')} />;
  }

  return <Dashboard onLogout={() => setAuthState('unauthenticated')} />;
}
```

- [ ] **Step 2: Frontend'i dev modda başlat ve test et**

```bash
# Terminal 1 - API
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet run --urls "http://0.0.0.0:5000"

# Terminal 2 - Frontend
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Web
npm run dev
```

Tarayıcıda `http://localhost:5173` aç.
- Login sayfası görünmeli
- `admin` / `admin123` ile giriş yap
- Dashboard açılmalı (UPS bağlantısı yoksa "BAĞLANTI YOK" banner'ı gösterir)

- [ ] **Step 3: Commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add UpsPoC.Web/src/App.tsx
git commit -m "feat: add auth guard routing in App.tsx"
```

---

## Task 18: Production Build Entegrasyonu

**Files:**
- Modify: `UpsPoC.Web/package.json` (build-and-copy script)

- [ ] **Step 1: package.json'a build-copy script ekle**

`UpsPoC.Web/package.json` içindeki `scripts` bölümüne şunu ekle:

```json
"build:prod": "vite build && xcopy /E /Y /I dist ..\\UpsPoC.Api\\wwwroot\\"
```

Tam `scripts` bölümü:
```json
"scripts": {
  "dev": "vite",
  "build": "tsc -b && vite build",
  "build:prod": "tsc -b && vite build && xcopy /E /Y /I dist ..\\UpsPoC.Api\\wwwroot\\",
  "lint": "eslint .",
  "preview": "vite preview"
}
```

- [ ] **Step 2: wwwroot klasörünü oluştur**

```bash
mkdir -p /c/Users/mehmet.erciyas/source/repos/UpsPoC/UpsPoC.Api/wwwroot
```

- [ ] **Step 3: Production build al**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Web
npm run build:prod
```

Expected: `dist/` oluşur ve `wwwroot/` kopyalanır.

- [ ] **Step 4: API'yi tek başına başlat ve tam test yap**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC\UpsPoC.Api
dotnet run --urls "http://0.0.0.0:5000"
```

Tarayıcıda `http://localhost:5000` aç — login sayfası görünmeli.
Başka bir cihazdaki tarayıcıdan `http://<bilgisayar-ip>:5000` aç — ağdan erişilebilir olmalı.

- [ ] **Step 5: Tüm testleri çalıştır**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
dotnet test
```

Expected: `Passed! - 4 tests`

- [ ] **Step 6: Final commit**

```bash
cd C:\Users\mehmet.erciyas\source\repos\UpsPoC
git add .
git commit -m "feat: production build integration, single-port deployment"
```

---

## Özet: Servis Başlatma

```bash
# Development (iki terminal)
cd UpsPoC.Api && dotnet run --urls "http://0.0.0.0:5000"
cd UpsPoC.Web && npm run dev    # http://localhost:5173

# Production (tek komut)
cd UpsPoC.Web && npm run build:prod
cd UpsPoC.Api && dotnet run --urls "http://0.0.0.0:5000"
# http://<ip>:5000 üzerinden erişin
```

## UPS Bağlantısı Doğrulama

İlk çalıştırmada UPS'e bağlantı test etmek için:

```bash
# SNMP GET ile ping benzeri test (snmpget kuruluysa)
snmpget -v2c -c public 192.168.143.246 1.3.6.1.2.1.33.1.1.2.0
```

Cevap geliyorsa SNMP bağlantısı çalışıyor. Dashboard "ONLİNE" göstermeli.
