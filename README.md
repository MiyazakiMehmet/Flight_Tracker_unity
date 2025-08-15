# Flight Tracker (Unity)

Track live or recorded flights on a 3D globe. This project reads flight states from a JSON file and renders airplane icons on a spherical Earth using **Unity**. It supports **multiple flights**, great‑circle waypoint pointing, and per-frame updates via a polling component.

> Components: `FlightDataGetter` (data input), `FlightManager` (spawning & wiring), `PlainTrack` (position & orientation on the globe).

---

## Table of Contents
- [Features](#features)
- [Project Structure](#project-structure)
- [Scene Setup](#scene-setup)
- [JSON Formats](#json-formats)
- [How It Works](#how-it-works)
- [License](#license)

---

## Features

- **Multiple flights**: Spawns one airplane per `flightNumber`.
- **Flexible JSON** input:
  - Wrapper: `{ "flights": [ ... ] }`
  - Bare array: `[ ... ]`
  - Single object: `{ ... }`
- **Great‑circle aiming** (optional): If a waypoint (arrival airport) is set, the plane nose points along the great‑circle direction to the target.
- **Heading on a sphere**: Without a waypoint, heading is applied **in the local tangent plane** (0° = North, 90° = East).
- **Event-driven updates**: `FlightDataGetter` periodically polls JSON and raises `OnFlightsUpdated`.

---

## Project Structure

```
Assets/
  Scripts/
    FlightManager.cs        // Spawns & manages planes per flightNumber
    FlightDataGetter.cs     // Polls JSON, raises OnFlightsUpdated
    PlainTrack.cs           // Places plane on the globe, applies rotation
  Prefabs/
    Plain.prefab            // Airplane icon prefab (must contain PlainTrack)
  Scenes/
    SampleScene.unity
```

**Key GameObjects (recommended):**
```
[Scene Root]
 ├── World (sphere mesh used as Earth)         ← set as Earth + World Parent
 └── Managers
      ├── FlightDataGetter (component)
      └── FlightManager   (component)
```

---

## Scene Setup

1. **World (Earth)**
   - A sphere mesh used as Earth. Its **Transform** is your local origin for lat/lon placement.
   - Keep a note of the sphere **local radius** (Unity default sphere is `0.5`).

2. **Managers**
   - Add an empty `Managers` GameObject.
   - Add **FlightDataGetter** and **FlightManager** components.

3. **Wire references (Inspector)**
   - **FlightManager**
     - `worldParent` → **World** (Hierarchy instance, not a prefab asset).
     - `earth`       → **World`** (same as above).
     - `airplanePrefab` → `Plain` prefab (Project asset).
     - `dataGetter`  → scene `FlightDataGetter` component.
   - **FlightDataGetter**
     - `airplane` → *(optional, legacy single-plane; leave empty for multi-plane)*
     - `path`     → JSON file path. If not rooted, it will be combined with `Application.persistentDataPath`.
     - `pollPeriod` → polling interval in seconds (e.g., `15`).

> **Important:** In the Inspector, always assign `World` from the **Hierarchy**, not from the **Project** window. Assigning a prefab asset here causes errors like *“Transform resides in a Prefab asset and cannot be set …”*.

---

## JSON Formats

`FlightDataGetter` accepts three shapes and normalizes internally:

### A) Wrapper (recommended)
```json
{
  "flights": [
    {
      "ok": true,
      "flightNumber": "TK123",
      "departureAirport": "IST",
      "arrivalAirport": "LHR",
      "departureTime": "2025-08-13T10:15:00Z",
      "arrivalTime": "2025-08-13T13:25:00Z",
      "lat": 41.2752,
      "lon": 29.1053,
      "heading": 252.0,
      "speed": 720.0,
      "estimatedArrival": "2025-08-13T13:25:00Z",
      "remainingMinutes": 105,
      "ts": 1723530000
    }
  ]
}
```

### B) Bare array
```json
[ { ... }, { ... } ]
```

### C) Single object
```json
{ ... }
```

**Field notes**
- `lat` (+N/−S), `lon` (+E/−W). West longitudes are **negative** (e.g., LAX ≈ `-118.4085`).
- `heading` uses aviation convention: **0° = North, 90° = East**.
- `ts` is a UNIX timestamp used for de‑duping. If `ts == 0`, the code falls back to tiny movement checks.

---

## How It Works

### FlightDataGetter
- Polls the JSON file at `pollPeriod` seconds.
- Parses into an array of `FlightState` (supports wrapper/array/single object).
- De‑dupes by `flightNumber` and `ts` (or tiny movement threshold).
- Updates `CurrentFlights` and invokes `OnFlightsUpdated(FlightState[])`.

### FlightManager
- Subscribes to `OnFlightsUpdated`.
- For each `flightNumber`, ensures a `Plain` instance exists:
  - If not, **instantiates** `airplanePrefab` under `worldParent` and assigns `earth` to its `PlainTrack`.
  - Calls `ApplyFlightState(lat, lon, heading, speed)` every update.
  - If `arrivalAirport` is known, sets a waypoint: `SetWaypoint(lat, lon)`.

### PlainTrack
- Converts `(lat, lon)` to a unit vector and places the plane at `earthLocalRadius + iconLocalOffset`.
- If waypoint exists → points along the **great‑circle** direction to the target.
- Else → builds a **tangent basis** (North/East/Up) and turns the nose using `heading` in the tangent plane.
- Uses exponential smoothing for position/rotation (`posLerp`, `rotLerp`).

---

## License

This repository is provided as-is for learning and demo purposes. Replace with your own license of choice.
