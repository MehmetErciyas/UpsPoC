# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UpsPoC is a full-stack UPS monitoring and control application. An ASP.NET Core 8 backend communicates with an East EA900 UPS over SNMP, while a React 19 + TypeScript frontend renders a real-time dashboard. Production runs as a single-port deployment (frontend built into `wwwroot`).

## Common Commands

### Backend (run from `UpsPoC.Api/`)
```bash
dotnet run --urls "http://0.0.0.0:5000"   # development
dotnet build                               # build only
dotnet test ../UpsPoC.Api.Tests            # run all tests
dotnet test ../UpsPoC.Api.Tests --filter "FullyQualifiedName~UpsDataServiceTests"  # single test class
```

### Frontend (run from `UpsPoC.Web/`)
```bash
npm run dev          # Vite dev server on port 5173 (proxies /api to :5000)
npm run build        # tsc type-check + vite build
npm run build:prod   # build + copy dist → ../UpsPoC.Api/wwwroot (single-port prod)
npm run lint         # ESLint with flat config
```

## Architecture

### Data Flow
```
Browser → (cookie auth) → ASP.NET Core API → SnmpService → UPS hardware
                                           ↓
                                    UpsDataService (in-memory history)
```

The frontend polls `GET /api/ups/status` on a configurable interval (5–60 s). Each call fetches live SNMP data **and** appends to the in-memory history queue. `GET /api/ups/history` returns the last 720 snapshots (~1 hour at 5 s).

### Backend Structure

**Services (singletons):**
- `SnmpService` — wraps Lextm.SharpSnmpLib, maps vendor-specific OIDs (Powerware/MAKELSAN NetAgent IX, enterprise 935) to `UpsStatus` fields. All SNMP calls use a `CancellationTokenSource` with timeout from config.
- `UpsDataService` — maintains a `ConcurrentQueue<UpsSnapshot>` capped at 720 entries. Handles disconnection by appending an "isConnected: false" snapshot.

**Controllers:**
- `AuthController` (`/api/auth`) — login/logout/me; sets 7-day HttpOnly cookie
- `UpsController` (`/api/ups`) — status, history, config read/write, command dispatch; all endpoints `[Authorize]`
- `DebugController` — SNMP walk and raw GET endpoints for OID discovery

**Key models:** `UpsStatus` (17 fields), `UpsCommand` (11 command types), `UpsConfig`, `AppSettings` (Ups + Auth sections).

### Frontend Structure

**State management:** `useUpsData` hook drives all polling, fetching, and state. Components are props-only; no global store.

**Component tree (Dashboard.tsx):**
- `MetricCard` — stat + progress bar
- `PowerChart` — Recharts line chart over history snapshots
- `CommandPanel` → `ConfirmDialog` — UPS control buttons with confirmation for destructive ops
- `ConfigModal` — read/write UPS config
- `AlarmPanel` — active alarms list
- `DeviceInfo` — device metadata

**API layer:** `UpsPoC.Web/src/api/client.ts` — typed `fetch` wrappers. In dev, Vite proxies `/api/*` to `http://localhost:5000`.

### Authentication

Cookie-based (HttpOnly, SameSite=Strict). Backend returns 401/403 JSON (no redirects) so the SPA handles auth state. Dev CORS allows only `localhost:5173`. Passwords stored as BCrypt hashes in `appsettings.json`.

## Configuration

`UpsPoC.Api/appsettings.json` contains all runtime config:
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

To change the password hash, generate one with BCrypt (cost factor 11) and update `PasswordHash`.

## Testing

6 xUnit + FluentAssertions unit tests in `UpsPoC.Api.Tests/Services/UpsDataServiceTests.cs` covering: FIFO behavior, 720-snapshot cap, and disconnection-snapshot handling. No frontend tests exist.

## SNMP Notes

- Protocol: SNMPv2c
- OIDs are vendor-specific (enterprise 935, Powerware/MAKELSAN NetAgent IX) — **not** standard RFC 1628 despite the code comment referencing it
- Use `DebugController` endpoints (`/api/debug/walk`, `/api/debug/get`) to discover OIDs when working with different UPS hardware
- UI language is Turkish
