using UnityEngine;
using System.IO;
using System.Collections.Generic;

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

    [System.Serializable] class WrapperList { public FlightState[] flights; }
    [System.Serializable] class WrapAnyArray { public FlightState[] w; } // düz dizi için

    [SerializeField] string path = "C:\\Users\\Rick Grimes\\Flight_Tracker\\Assets\\JsonFiles\\FlightState.json";
    [SerializeField] float pollPeriod = 15f;

    [Header("Receiver (single-legacy)")]
    public PlainTrack airplane; // tek uçak için hâlâ destekliyoruz (boş bırakılabilir)

    public FlightState[] CurrentFlights { get; private set; } = System.Array.Empty<FlightState>();
    public System.Action<FlightState[]> OnFlightsUpdated;

    const float MinDeltaDeg = 1e-4f; // ~11 m

    // her uçuş için son ts/lat/lon
    readonly Dictionary<string, (long ts, float lat, float lon)> _seen = new();

    void Start()
    {
        // Path zaten mutlaksa bozma
        if (!Path.IsPathRooted(path))
            path = System.IO.Path.Combine(Application.persistentDataPath, path);

        Debug.Log($"[Poller] Full path: {path}");
        InvokeRepeating(nameof(PollOnce), 0f, pollPeriod);
    }

    public void PollOnce()
    {
        if (!File.Exists(path)) { Debug.LogWarning($"[Poller] Not found: {path}"); return; }

        string txt;
        try { txt = File.ReadAllText(path); }
        catch (System.Exception e) { Debug.LogError($"[Poller] Read failed: {e.Message}"); return; }

        FlightState[] flights = null;

        // 1) wrapper: { "flights": [...] }
        try
        {
            var wrapped = JsonUtility.FromJson<WrapperList>(txt);
            if (wrapped != null && wrapped.flights != null) flights = wrapped.flights;
        }
        catch { /* geç */ }

        // 2) düz dizi: [ {...}, {...} ]  -> {"w":[...]} hilesi
        if (flights == null)
        {
            try
            {
                var wrapped = JsonUtility.FromJson<WrapAnyArray>("{\"w\":" + txt + "}");
                if (wrapped != null && wrapped.w != null) flights = wrapped.w;
            }
            catch { /* geç */ }
        }

        // 3) tek obje (eski format) -> listeye sar
        if (flights == null)
        {
            try
            {
                var single = JsonUtility.FromJson<WrapAnyArray>("{\"w\":[" + txt + "]}");
                if (single != null && single.w != null) flights = single.w;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Poller] JSON parse error: {e.Message}");
                return;
            }
        }

        if (flights == null || flights.Length == 0)
        {
            Debug.LogWarning("[Poller] No flights found.");
            return;
        }

        // de-dupe ve log
        var list = new List<FlightState>(flights.Length);
        foreach (var st in flights)
        {
            if (st == null || string.IsNullOrEmpty(st.flightNumber)) continue;

            // uçuşa özel son kayıt
            if (!_seen.TryGetValue(st.flightNumber, out var last))
            {
                list.Add(st);
                _seen[st.flightNumber] = (st.ts, st.lat, st.lon);
            }
            else
            {
                bool sameTs = (st.ts != 0 && st.ts == last.ts);
                bool tinyMove = (st.ts == 0 &&
                    Mathf.Abs(st.lat - last.lat) < MinDeltaDeg &&
                    Mathf.Abs(st.lon - last.lon) < MinDeltaDeg);

                if (!sameTs && !tinyMove)
                {
                    list.Add(st);
                    _seen[st.flightNumber] = (st.ts, st.lat, st.lon);
                }
            }
        }

        if (list.Count == 0) return;

        CurrentFlights = list.ToArray();

        // Eski tek-uçak akışını bozmamak için: ilk uçuşu tek alıcıya gönder
        if (airplane != null)
        {
            var st = CurrentFlights[0];
            airplane.ApplyFlightState(st.lat, st.lon, st.heading, st.speed);
        }

        // Yayınla
        OnFlightsUpdated?.Invoke(CurrentFlights);
    }
}
