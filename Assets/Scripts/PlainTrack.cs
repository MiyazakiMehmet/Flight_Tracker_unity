using UnityEngine;

/// Uçak ikonunu Dünya yüzeyine yapıştırır ve heading'e göre döndürür.
/// - ApplyFlightState(...) ile anlık lat/lon/heading/hız güncellenir.
/// - SetWaypoint(lat, lon) ile hedefe (great-circle) doğru ilerler.
/// - Dünya döndürülse de ikon yerinde kalır (her frame Earth.TransformPoint/Direction).
public class PlainTrack : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Dünya küresi (Sphere vb.)")]
    public Transform earth;

    [Header("Earth (LOCAL space)")]
    [Tooltip("Earth modelinin LOCAL yarıçapı. Unity Sphere için 0.5'tir.")]
    public float earthLocalRadius = 0.5f;       // Unity default Sphere -> 0.5
    [Tooltip("İkonu zeminden hafif kaldır (LOCAL).")]
    public float iconLocalOffset = 0.002f;

    [Header("Smoothing")]
    [Tooltip("Pozisyon interpolasyonu (daha yüksek = daha hızlı takip)")]
    public float posLerp = 10f;
    [Tooltip("Rotasyon interpolasyonu (daha yüksek = daha hızlı takip)")]
    public float rotLerp = 8f;

    [Header("Model axes / reference")]
    [Tooltip("Fallback: modelde 'burun' yerel yönü (Quad/Plane için çoğu zaman +Z)")]
    public Vector3 modelNoseLocal = Vector3.forward;
    [Tooltip("Fallback: model yüzey normalinin yerel yönü (Unity Plane için çoğu zaman +Y)")]
    public Vector3 modelNormalLocal = Vector3.up;
    [Tooltip("Burnun ucuna koyduğun boş obje. Mavi ok (Z+) burnu göstermeli.")]
    public Transform noseReference;

    [Header("Heading tweak")]
    [Tooltip("Heading üzerine eklenecek ince ayar (derece). 90/-90/180 gerekebilir.")]
    public float headingOffsetDeg = 0f;

    [Header("Movement / Time")]
    [Tooltip("JSON akmazsa heading yönünde kendi kendine ilerlesin (waypoint kapalıyken).")]
    public bool simulateMovement = false;
    [Tooltip("Dünyanın km cinsinden yarıçapı (küre üzerinde adım hesabı için).")]
    public float earthWorldRadiusKm = 6371f;
    [Tooltip("Görsel hız için zaman sıkıştırma (1=gerçek zaman, 600=10dk/sn).")]
    public float timeCompression = 600f;

    [Header("Waypoint mode")]
    [Tooltip("Hedef enlem/boylama doğru great-circle ilerle.")]
    public bool useWaypoint = false;
    public float waypointLatDeg;
    public float waypointLonDeg;
    [Tooltip("Hedefe ulaştı saymak için açısal tolerans (derece) ~2.2km ≈ 0.02°")]
    public float arriveEpsilonDeg = 0.02f;

    // ---- internal state ----
    float _latDeg, _lonDeg, _headingDeg, _speedKmh;
    bool _hasState;

    Vector3 _targetPosWS;
    Quaternion _targetRotWS;


    /// JSON/Poller burayı çağırır
    public void ApplyFlightState(float latDeg, float lonDeg, float headingDeg, float speedKmh)
    {
        _latDeg = latDeg;
        _lonDeg = lonDeg;
        _headingDeg = headingDeg;
        _speedKmh = speedKmh;
        _hasState = true;
    }

    /// Hedef noktaya yönelmek için
    public void SetWaypoint(float latDeg, float lonDeg)
    {
        waypointLatDeg = latDeg;
        waypointLonDeg = lonDeg;
        useWaypoint = true;
    }

    void LateUpdate()
    {
        if (!earth || !_hasState) return;

        // 1) İlerleme (hedef varsa hedefe; yoksa opsiyonel simülasyon)
        if (useWaypoint) StepTowardsWaypoint(Time.deltaTime);
        else if (simulateMovement && _speedKmh > 0f) StepOnSphere(Time.deltaTime);

        // 2) Earth LOCAL uzayında lat/lon -> nokta
        Vector3 local = LatLonToLocal(_latDeg, _lonDeg, earthLocalRadius + iconLocalOffset);

        // 3) Dünya döndükçe world pozisyonu değişsin
        Vector3 posWS = earth.TransformPoint(local);

        // 4) O noktadaki normal + coğrafi Doğu/Kuzey (her kare türet)
        Vector3 upWS = (posWS - earth.position).normalized;            // yüzey normali
        Vector3 eastWS = Vector3.Cross(Vector3.up, upWS).normalized;     // Doğu
        if (eastWS.sqrMagnitude < 1e-6f) eastWS = Vector3.Cross(Vector3.forward, upWS).normalized; // kutup koruması
        Vector3 northWS = Vector3.Cross(upWS, eastWS).normalized;        // Kuzey

        // 5) Heading (0°=Kuzey, 90°=Doğu)
        float h = (_headingDeg + headingOffsetDeg) * Mathf.Deg2Rad;
        Vector3 fwdWS = Mathf.Sin(h) * eastWS + Mathf.Cos(h) * northWS;

        // 6) Model eksenlerini dünya eksenleriyle eşle
        Quaternion worldRot = Quaternion.LookRotation(fwdWS, upWS);
        Quaternion modelBasis = Quaternion.LookRotation(modelNoseLocal, modelNormalLocal);
        Quaternion rotWS = worldRot * Quaternion.Inverse(modelBasis);

        // 7) Yumuşat ve uygula
        _targetPosWS = posWS;
        _targetRotWS = rotWS;
        transform.position = Vector3.Lerp(transform.position, _targetPosWS, 1f - Mathf.Exp(-posLerp * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotWS, rotLerp * Time.deltaTime);

        // Debug:
        // Debug.DrawRay(transform.position, transform.forward * 1.0f, Color.cyan);
    }

    // === Movement helpers ===

    // Enlem-boylam -> Earth LOCAL konumu
    static Vector3 LatLonToLocal(float latDeg, float lonDeg, float r)
    {
        float lat = latDeg * Mathf.Deg2Rad;
        float lon = lonDeg * Mathf.Deg2Rad;
        float x = r * Mathf.Cos(lat) * Mathf.Cos(lon);
        float y = r * Mathf.Sin(lat);
        float z = r * Mathf.Cos(lat) * Mathf.Sin(lon);
        return new Vector3(x, y, z);
    }

    // Birim Earth LOCAL vektör (yarıçap=1)
    static Vector3 LatLonUnitLocal(float latDeg, float lonDeg)
    {
        float lat = latDeg * Mathf.Deg2Rad;
        float lon = lonDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon),
                           Mathf.Sin(lat),
                           Mathf.Cos(lat) * Mathf.Sin(lon)).normalized;
    }

    // JSON akmazsa: sabit heading + hızla küre üzerinde adım (EARTH LOCAL)
    void StepOnSphere(float dt)
    {
        Vector3 uL = LatLonUnitLocal(_latDeg, _lonDeg);              // local up (normal)
        Vector3 eastL = Vector3.Cross(Vector3.up, uL).normalized;    // Doğu
        if (eastL.sqrMagnitude < 1e-6f) eastL = Vector3.Cross(Vector3.forward, uL).normalized;
        Vector3 northL = Vector3.Cross(uL, eastL).normalized;        // Kuzey

        float h = _headingDeg * Mathf.Deg2Rad;
        Vector3 fwdL = Mathf.Sin(h) * eastL + Mathf.Cos(h) * northL; // tanjant ileri
        Vector3 axisL = Vector3.Cross(fwdL, uL).normalized;           // büyük çember ekseni

        float stepDeg = (_speedKmh / 3600f) / Mathf.Max(1f, earthWorldRadiusKm)
                        * Mathf.Rad2Deg * dt * timeCompression;

        Vector3 uL2 = Quaternion.AngleAxis(stepDeg, axisL) * uL;
        WriteLatLonFromUnit(uL2);
        // Heading sabit kalır; gerçek kurs waypoint modunda güncellenir.
    }

    // Waypoint’e doğru great-circle adımı (EARTH LOCAL) — hedefe giderken heading'i (course) günceller
    void StepTowardsWaypoint(float dt)
    {
        Vector3 u = LatLonUnitLocal(_latDeg, _lonDeg);
        Vector3 v = LatLonUnitLocal(waypointLatDeg, waypointLonDeg);

        float dot = Mathf.Clamp(Vector3.Dot(u, v), -1f, 1f);
        float angToGo = Mathf.Acos(dot) * Mathf.Rad2Deg; // derece

        if (angToGo < arriveEpsilonDeg)
        {
            _latDeg = waypointLatDeg;
            _lonDeg = waypointLonDeg;
            useWaypoint = false;
            return;
        }

        // Büyük çember ekseni: u × v
        Vector3 axis = Vector3.Cross(u, v).normalized;

        // Bu karede atılacak adım (derece)
        float stepDeg = (_speedKmh / 3600f) / Mathf.Max(1f, earthWorldRadiusKm)
                        * Mathf.Rad2Deg * dt * timeCompression;
        stepDeg = Mathf.Min(stepDeg, angToGo); // hedefi aşma

        // u'yu axis etrafında ileri doğru döndür
        Vector3 u2 = Quaternion.AngleAxis(stepDeg, axis) * u;
        WriteLatLonFromUnit(u2);

        // Gerçek kurs: büyük çember teğeti t = axis × u2
        Vector3 t = Vector3.Cross(axis, u2).normalized;

        // O noktadaki East/North bazlarını türet
        Vector3 east = Vector3.Cross(Vector3.up, u2).normalized;
        if (east.sqrMagnitude < 1e-6f) east = Vector3.Cross(Vector3.forward, u2).normalized;
        Vector3 north = Vector3.Cross(u2, east).normalized;

        // Heading = atan2( proj_east, proj_north )  (0°=Kuzey, 90°=Doğu)
        float hdgRad = Mathf.Atan2(Vector3.Dot(t, east), Vector3.Dot(t, north));
        _headingDeg = hdgRad * Mathf.Rad2Deg;
    }

    void WriteLatLonFromUnit(Vector3 uL)
    {
        _latDeg = Mathf.Asin(Mathf.Clamp(uL.y, -1f, 1f)) * Mathf.Rad2Deg;
        _lonDeg = Mathf.Atan2(uL.z, uL.x) * Mathf.Rad2Deg;
    }
}
