using UnityEngine;
using System.Collections.Generic;

public class FlightManager : MonoBehaviour
{
    [Header("Scene/Hierarchy")]
    public Transform worldParent; // World
    public Transform earth;       // World

    [Header("Prefabs")]
    public GameObject airplanePrefab;

    [Header("Data")]
    public FlightDataGetter dataGetter;

    // flightNumber -> PlainTrack
    private readonly Dictionary<string, PlainTrack> _planes = new();

    void Start()
    {
        if (dataGetter == null) { Debug.LogError("[FM] dataGetter yok."); return; }
        if (worldParent == null) { Debug.LogError("[FM] worldParent (World) atanmamış."); return; }
        if (earth == null) earth = worldParent;
        if (airplanePrefab == null) { Debug.LogError("[FM] airplanePrefab yok."); return; }

        dataGetter.OnFlightsUpdated += OnFlightsUpdated;
    }

    void OnDestroy()
    {
        if (dataGetter != null) dataGetter.OnFlightsUpdated -= OnFlightsUpdated;
    }

    void OnFlightsUpdated(FlightDataGetter.FlightState[] flights)
    {
        foreach (var st in flights)
        {
            if (st == null || string.IsNullOrEmpty(st.flightNumber)) continue;

            // Uçak var mı?
            if (!_planes.TryGetValue(st.flightNumber, out var pt) || pt == null)
            {
                // Spawn
                var go = Instantiate(airplanePrefab, worldParent, false);
                pt = go.GetComponent<PlainTrack>();
                if (pt == null) { Debug.LogError("[FM] Prefab’ta PlainTrack yok!"); Destroy(go); continue; }
                if (pt.earth == null) pt.earth = earth;

                _planes[st.flightNumber] = pt;
                // İsimde flight no görmek istersen:
                go.name = $"Plane_{st.flightNumber}";
            }

            // Güncelle
            pt.ApplyFlightState(st.lat, st.lon, st.heading, st.speed);

            // (Opsiyonel) Varışa waypoint
            if (!string.IsNullOrEmpty(st.arrivalAirport) &&
                TryGetAirportLatLon(st.arrivalAirport, out float alat, out float alon) &&
                (Mathf.Abs(alat) > 1e-6f || Mathf.Abs(alon) > 1e-6f))
            {
                pt.SetWaypoint(alat, alon);
            }
        }

        // (Opsiyonel) listede artık gelmeyen uçakları gizlemek/silmek istersen burada yönetebilirsin.
    }

    // Kısa bir havalimanı sözlüğü (gerekirse genişlet)
    static bool TryGetAirportLatLon(string code, out float lat, out float lon)
    {
        switch (code.ToUpperInvariant())
        {
            case "LHR": lat = 51.4700f; lon = -0.4543f; return true;
            case "LGW": lat = 51.1537f; lon = -0.1821f; return true;
            case "STN": lat = 51.8850f; lon = 0.2350f; return true;
            case "LTN": lat = 51.8747f; lon = -0.3683f; return true;
            case "MAN": lat = 53.3650f; lon = -2.2725f; return true;
            case "EDI": lat = 55.9500f; lon = -3.3725f; return true;
            case "IST": lat = 41.2753f; lon = 28.7519f; return true;
            case "FRA": lat = 50.0379f; lon = 8.5622f; return true;
            case "LAX": lat = 33.9416f; lon = -118.4085f; return true;
            default: lat = lon = 0f; return false;
        }
    }
}
