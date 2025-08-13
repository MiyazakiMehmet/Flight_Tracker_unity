using UnityEngine;
using System.IO;
using System.Globalization;


public class FlightDataGetter : MonoBehaviour
{
    [System.Serializable]
    public class FlightState
    {
        public bool ok;
        public string flightNumber;
        public string departureAirport;
        public string arrivalAirport;
        public string departureTime;
        public string arrivalTime;
        public float lat;
        public float lon;
        public float heading;
        public float speed;
        public string estimatedArrival;
        public int remainingMinutes;
        public long ts;
    }




    [SerializeField] string path = "C:\\Users\\Rick Grimes\\Flight_Tracker\\Assets\\JsonFiles\\FlightState.json";
    [SerializeField] float pollPeriod = 15f;
    
    [Header("Receiver")]
    public PlainTrack airplane;

    void Start()
    {
        path = System.IO.Path.Combine(Application.persistentDataPath, path);
        Debug.Log($"[Poller] Full path: {path}");
        InvokeRepeating(nameof(PollOnce), 0f, pollPeriod);
    }


    static bool TryGetAirportLatLon(string code, out float lat, out float lon)
    {
        switch (code.ToUpperInvariant())
        {
            case "LHR": lat = 58.4700f; lon = -0.7543f; return true; // London Heathrow
            case "LGW": lat = 51.1537f; lon = -0.1821f; return true; // Gatwick
            case "STN": lat = 51.8850f; lon = 0.2350f; return true;  // Stansted
            case "LTN": lat = 51.8747f; lon = -0.3683f; return true; // Luton
            case "MAN": lat = 53.3650f; lon = -2.2725f; return true; // Manchester
            case "EDI": lat = 55.9500f; lon = -3.3725f; return true; // Edinburgh
            case "IST": lat = 41.2753f; lon = 28.7519f; return true; // İstanbul
            // ihtiyaca göre ekle
            default: lat = lon = 0f; return false;
        }
    }



    long _lastTs = -1;
    float _lastLat, _lastLon;
    const float MinDeltaDeg = 1e-4f; // ~11 m

    string _lastArrivalCode = null;
    bool _waypointSetForArrival = false;

    void PollOnce()
    {
        if (!File.Exists(path)) { Debug.LogWarning($"[Poller] Not found: {path}"); return; }

        string txt;
        try { txt = File.ReadAllText(path); }
        catch (System.Exception e) { Debug.LogError($"[Poller] Read failed: {e.Message}"); return; }

        FlightState st;
        try { st = JsonUtility.FromJson<Wrapper>("{\"w\":" + txt + "}").w; }
        catch (System.Exception e) { Debug.LogError($"[Poller] JSON parse error: {e.Message}"); return; }

        if (st == null || !st.ok) { Debug.LogWarning("[Poller] ok=false or null"); return; }

        // === de-dupe: aynı timestamp geldiyse atla ===
        if (st.ts != 0 && st.ts == _lastTs) return;
        if (st.ts == 0)
        {
            if (Mathf.Abs(st.lat - _lastLat) < MinDeltaDeg &&
                Mathf.Abs(st.lon - _lastLon) < MinDeltaDeg)
                return;
        }

        // Konsola özet
        Debug.Log($"[Poller] {st.flightNumber} {st.departureAirport}->{st.arrivalAirport}  HDG:{st.heading:F0}  SPD:{st.speed:F0}km/h");

        // Uçağa state gönder (PlainTrack ApplyFlightState)
        if (airplane != null)
            airplane.ApplyFlightState(st.lat, st.lon, st.heading, st.speed);

        _lastTs = st.ts;
        _lastLat = st.lat;
        _lastLon = st.lon;

        // === ARRIVAL’dan OTO WAYPOINT ===
        // 0,0'a asla gitme + aynı hedefi tekrar tekrar kurma
        if (airplane != null && !string.IsNullOrEmpty(st.arrivalAirport))
        {
            // Arrival code değiştiyse, yeni hedef kur
            if (!_waypointSetForArrival || !string.Equals(_lastArrivalCode, st.arrivalAirport, System.StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetAirportLatLon(st.arrivalAirport, out float alat, out float alon))
                {
                    // 0,0 guard
                    if (Mathf.Abs(alat) > 1e-6f || Mathf.Abs(alon) > 1e-6f)
                    {
                        airplane.SetWaypoint(alat, alon);
                        // canlı veri varken simülasyona gerek yok:
                        airplane.simulateMovement = false;

                        Debug.Log($"[Poller] Waypoint set from arrivalAirport {st.arrivalAirport}: lat={alat:F4}, lon={alon:F4}");
                        _waypointSetForArrival = true;
                        _lastArrivalCode = st.arrivalAirport;
                    }
                    else
                    {
                        Debug.LogWarning($"[Poller] Arrival {st.arrivalAirport} resolved to (0,0). Waypoint ignored.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Poller] Unknown arrival airport code '{st.arrivalAirport}'. Add it to TryGetAirportLatLon.");
                }
            }
        }
    }


    [System.Serializable] class Wrapper { public FlightState w; }
}
