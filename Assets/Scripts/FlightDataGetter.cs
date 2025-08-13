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
            case "LHR": lat = 51.4700f; lon = -0.4543f; return true; // London Heathrow
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

        // Konsola özet (isteğe bağlı)
        Debug.Log($"[Poller] {st.flightNumber} {st.departureAirport}->{st.arrivalAirport}  HDG:{st.heading:F0}  SPD:{st.speed:F0}km/h");

        // 🔴 Asıl nokta: PlainTrack’e ver
        if (airplane != null) airplane.ApplyFlightState(st.lat, st.lon, st.heading, st.speed);
    }

    [System.Serializable] class Wrapper { public FlightState w; }
}
