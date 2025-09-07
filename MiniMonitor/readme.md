# MiniMonitor

A lightweight Photino.NET desktop dashboard providing at-a-glance status for:
- Upcoming calendar events (from one or more remote .ics feeds)
- Local system stats (CPU + GPU via LibreHardwareMonitor)
- Current weather (OpenWeatherMap API)
- YouTube Music playback status & control (via injected automation + DevTools / Selenium style integration)
- Basic smart device / light toggle actions

The UI (index-b.html) is a 1920x720 chromeless window intended for an auxiliary display (e.g., ultra‑wide top bar / wall monitor / mini panel).

---
## Key Features
- Photino.NET window (chromeless, fixed size) hosting a local HTML/TypeScript UI
- Embedded Kestrel server (loopback:9191) for:
  - Aggregated polling endpoint (`GET /mini-monitor?action=sensorData`)
  - Music data ingestion (`POST /mini-monitor?action=putMusicData`)
  - Music control relay (`POST /mini-monitor?action=musicControl` + WebSocket push)
  - Calendar refresh trigger (`POST /mini-monitor?action=RefreshCalendar`)
  - IPC style commands (close app, wiring up YouTube Music)
  - WebSocket endpoint (`/ws`) for near‑real‑time music control events
- Background worker threads:
  - Calendar fetch & parse (Ical.Net)
  - Weather fetch (OpenWeatherMap, 10‑minute cadence)
  - Sensor telemetry (LibreHardwareMonitor; GPU + CPU usage/clock/temp)
  - Window state maintenance (prevents minimize, restores position)
- TypeScript front-end (auto‑compiled) handles:
  - Poll loop (1s) to `/mini-monitor?action=sensorData`
  - Visual alarm logic & blinking for upcoming meeting windows
  - Weather + icon mapping (OpenWeather conditions → local icon / remote icon)
  - Music control (play/pause/next) via server relay + WebSocket
  - Audio scheduling for meeting alerts (progressively more urgent)
  - Light / device toggles via local network HTTP

---
## Technology Stack
| Area | Tech / Library |
|------|----------------|
| Desktop host | Photino.NET |
| Web server / IPC | Minimal API (Kestrel) on loopback:9191 |
| Calendar | Ical.Net |
| Weather | OpenWeatherMap REST API |
| Hardware telemetry | LibreHardwareMonitorLib |
| Music control (browser) | DevTools / Selenium (ChromeDriver / EdgeDriver / WebDriver packages) + injected JS |
| Scripting / UI | TypeScript (wwwroot/ts), compiled to JS (wwwroot/assets) |
| Date/Time formatting | Luxon (CDN) |
| DOM utilities | jQuery (CDN) |

NuGet packages (see .csproj): Ical.Net, LibreHardwareMonitorLib, Photino.NET, Selenium WebDriver + drivers, InputSimulator, System.Management.

---
## Directory Overview
```
MiniMonitor/
  Program.cs                 <-- App bootstrap & all background threads / server
  WeatherFetcher.cs          <-- Weather + Geo lookup wrapper
  Win32.cs                   <-- Native helpers (window positioning / state)
  wwwroot/
    index-b.html             <-- Primary dashboard UI
    index.html / index-c.html (legacy / alternates)
    ts/main.ts               <-- Main TypeScript source (edit here – NOT the compiled JS)
    assets/                  <-- Icons, audio, styles, compiled JS (main.js), css
```

---
## Runtime Data Flow
1. Program.cs creates PhotinoWindow and loads `wwwroot/index-b.html`.
2. Background threads begin collecting:
   - Calendar: Each configured ICS feed downloaded; next non‑expired event (± window) selected.
   - Sensors: LibreHardwareMonitor enumerates CPU & GPU sensors each second.
   - Weather: Location (zip→lat/lon) resolved once, then weather every 10 minutes.
3. Data pushed to UI by two mechanisms:
   - Direct `window.SendWebMessage()` (legacy / some code paths)
   - Aggregated polling endpoint consumed by `main.ts` (`/mini-monitor?action=sensorData`).
4. Music integration:
   - Browser automation (Edge/Chrome app window) is launched (manual trigger) and JS injected (main.js capabilities).
   - Front-end script in the YouTube Music context posts music state back (`putMusicData`) + WebSocket live control.
5. UI logic applies visual + audible alerts for meetings based on minutes-until thresholds.

---
## IPC & HTTP / WebSocket Endpoints
| Method | Endpoint | Query / Action | Purpose |
|--------|----------|----------------|---------|
| GET | /mini-monitor | action=sensorData | Returns combined JSON: sensorData, calendarData, weatherData, musicData |
| POST | /mini-monitor | action=putMusicData | Accepts JSON MusicData payload from injected YT Music JS |
| POST | /mini-monitor | action=musicControl | Queues a JSON control instruction broadcast via WebSocket (play/pause/next) |
| POST | /mini-monitor | action=wireUpYT | Invokes EdgeDevToolsAutomation to attach/inject JavaScript |
| POST | /mini-monitor | action=RefreshCalendar | Signals calendar thread via AutoResetEvent |
| POST | /mini-monitor | action=CloseApp | Graceful application shutdown |
| GET/POST | /mini-monitor | (others) | Basic passthrough / legacy message forwarding |
| GET (WS) | /ws | (WebSocket) | Bi‑directional music control channel |

WebSocket messages (client→UI) are plain JSON with fields like: `{ "action": "pause" }`, `{ "action": "next" }`.

---
## Message / DataType Contracts
Front-end & back-end exchange JSON objects that include a `DataType` discriminator.

| DataType | Shape (selected fields) | Notes |
|----------|-------------------------|-------|
| SensorData | `{ DataType, temp, gpuLoad, coreClock, memClock, cpuTotal }` | GPU temperature/load + CPU total load |
| CalendarData | `{ DataType, HasEvents, Summary?, StartTimeUtc? }` | Missing/false if no near-term events |
| WeatherData | `{ DataType, City, Temperature, Description }` | Description matched to icon mapping array in TS |
| MusicData | `{ DataType, Title, Artist, Album?, AlbumArtUrl?, PlayerState }` | PlayerState numeric enum (-1..5) |
| MusicUpdate | Wrapper for incremental YT updates (legacy) |

PlayerState enum (Program.cs): `UNSTARTED=-1, ENDED=0, PLAYING=1, PAUSED=2, BUFFERING=3, CUED=5`.

---
## Configuration (config.json)
A `config.json` file (created after first save or manually) in the application root stores window position & service credentials.

Example:
```json
{
  "x": 0,
  "y": 0,
  "icsFiles": [
    "https://example.com/personal.ics",
    "https://example.com/work.ics"
  ],
  "openWeatherMapApi": "YOUR_OPENWEATHERMAP_KEY",
  "personalMailUserId": "user@example.com",
  "personalMailPassword": "PLAINTEXT_PASSWORD"
}
```
Notes:
- Storing plaintext credentials is NOT recommended for production. For personal / offline kiosk use only.
- `icsFiles` may contain multiple remote calendar feeds (HTTP(S) .ics URLs).
- `x`, `y` = last saved window top-left coordinates (Move/Save commands modify these).

---
## Building & Running
Prerequisites:
- .NET 8 SDK
- Windows (due to Win32 API usage & LibreHardwareMonitor)
- Chrome or Edge installed (for music automation; ChromeDriver / EdgeDriver provided via NuGet)
- OpenWeatherMap API key

Steps:
1. Clone repository
2. Create `config.json` with at least `openWeatherMapApi` + one entry in `icsFiles` (or run once and edit generated file)
3. Run:
```
dotnet run -p MiniMonitor
```
4. The dashboard window appears (1920x720). Place / save position as desired.

Optional (YouTube Music wiring): Press the Fix button (or action that triggers `wireUpYT`) to attach/inject script and begin music reporting.

---
## Front-End Development Notes
- Edit ONLY TypeScript under `wwwroot/ts/main.ts`.
- The environment auto-compiles TS → `wwwroot/assets/main.js` (do NOT hand-edit compiled JS).
- UI uses polling rather than push for most data (except music control via WebSocket) to simplify logic & avoid threading concerns.
- Time formatting & relative strings rely on Luxon (CDN loaded in HTML head).

### Visual Meeting Alerts (main.ts)
The logic grades urgency by minutes until meeting and:
- Adds CSS borders / background tints
- Schedules blinking cadence (setInterval + opacity flicker)
- Schedules audio beeps via `AudioScheduler` with decreasing intervals as event nears

Threshold summary (approx):
- <= 30m: light dashed border, slow flash, audio every 10m
- <= 10m: faster flash, audio every 3m
- <= 5m: frequent flash, audio every 1–3m
- <= 1–2m: rapid flash + immediate audio

---
## Window / Position Control
Internal commands handled in Program.cs (via `HandleMessage`):
- `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown` (pixel nudges + save)
- `SavePosition` (persists current `x`,`y`)
- `ToggleYTM` (show/hide or (re)launch music window)
- `Close` (terminate application)

---
## Security & Privacy Considerations
- Plaintext credentials (mail) & API keys stored locally: treat machine as trusted / offline.
- Local server binds only to `Loopback` (127.0.0.1) reducing remote attack surface.
- No authentication on endpoints; do not expose port 9191 beyond localhost.
- Injected automation & window control rely on WebDriver and Win32 – avoid running untrusted builds.

---
## Troubleshooting
| Symptom | Possible Cause | Remedy |
|---------|----------------|--------|
| Weather never updates | Missing / invalid OpenWeatherMap key | Set `openWeatherMapApi` in config.json |
| No calendar events | Incorrect ICS URL / network error | Verify URL in browser; check log.txt |
| GPU/CPU blank | Sensors not resolved / hardware type mismatch | Ensure LibreHardwareMonitorLib supports your GPU; restart app |
| Music controls do nothing | Injection not completed / driver not launched | Use Fix / wireUpYT action again; verify Chrome/Edge installed |
| Meeting alerts not flashing | No upcoming event / StartTime in past | Confirm ICS times & timezone correctness |

Logs: A simple rolling log is appended to `log.txt` in app directory.

---
## Extensibility Ideas
- Replace polling with fully push-based (WebSocket) model
- Modularize Program.cs into services / DI container
- Add configurable thresholds / sound themes
- OAuth for calendar & mail instead of raw ICS + credentials
- Cross‑platform abstractions (Linux sensor alternative, window mgmt removal)
- Package as single-file trimmed self‑contained publish

---
## Disclaimer
This project is tailored for a personal always-on dashboard. Security hardening, credential protection, and multi-user scenarios are out of scope in the current codebase. Use at your own risk.

---
## Quick Reference
| Area | File |
|------|------|
| Main entry | Program.cs |
| UI markup | wwwroot/index-b.html |
| TS logic | wwwroot/ts/main.ts |
| Weather | WeatherFetcher.cs |
| Config | config.json (manual) |
| Logs | log.txt |

---
Happy monitoring!