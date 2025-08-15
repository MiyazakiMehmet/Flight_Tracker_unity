using UnityEngine;

public class PlainTrack : MonoBehaviour
{
    // ===== Earth & Visual =====
    [Header("Earth & Visual")]
    public Transform earth;                 // Earth pivot (Rotation=0,0,0 önerilir)
    public float earthLocalRadius = 0.5f;   // Unity Sphere default
    public float iconLocalOffset = 0.002f;  // yüzeyden hafif kaldır

    [Header("Smoothing")]
    public float posLerp = 10f;
    public float rotLerp = 8f;

    [Header("Model Axes")]
    public Vector3 modelNoseLocal = Vector3.forward; // +Z: burun
    public Vector3 modelNormalLocal = Vector3.up;    // +Y: yüzey normali

    // ===== Heading & Speed Input =====
    [Header("Heading Input Format")]
    [Tooltip("Feed 0°=Doğu ise aç (navigasyonda 0°=Kuzey).")]
    public bool headingZeroAtEast = false;
    [Tooltip("Feed CCW artıyorsa aç (CW’a çevirir).")]
    public bool headingCounterClockwise = false;
    [Tooltip("Feed 180° ters ise aç.")]
    public bool headingAdd180 = false;
    [Tooltip("Ek ince ayar (derece).")]
    public float headingOffsetDeg = 0f;

    public enum SpeedUnit { Kmh, Ms, Knots }
    [Header("Speed Input")]
    public SpeedUnit inputSpeedUnit = SpeedUnit.Kmh;

    // ===== Movement =====
    [Header("Movement / Time")]
    [Tooltip("JSON yokken sabit heading ile ilerle.")]
    public bool simulateMovement = false;
    public float earthWorldRadiusKm = 6371f;
    public float timeCompression = 60f;

    [Header("Visual root (nose points +Z)")]
    public Transform airplaneRoot; // Inspector’dan atadığın 'airplaneroot' objesi

    [Header("Waypoint Mode")]
    public bool useWaypoint = false;
    public float waypointLatDeg;
    public float waypointLonDeg;
    [Tooltip("Hedef kabul eşiği (derece). ~0.1° ≈ 11 km")]
    public float arriveEpsilonDeg = 0.10f;

    [Header("External Update Control")]
    [Tooltip("Simulate/Waypoint açıkken dış pozisyonu yok say (geri atmayı önler).")]
    public bool ignoreExternalPosWhileMoving = true;

    [Header("Step Limits")]
    [Tooltip("Bir framede max merkez açı (derece).")]
    public float maxStepDegPerFrame = 0.7f;

    // ===== Map Alignment (Tek yöntem: Offset) =====
    [Header("Map Alignment (use ONLY offsets; keep Earth rotation = 0,0,0)")]
    [Tooltip("Kuzey +Y yönünde ise true (kutuplar ±Y).")]
    public bool northAtPositiveY = true;
    [Tooltip("Harita lat ofseti (+=kuzeye, derece).")]
    public float earthLatitudeOffsetDeg = 0f;
    [Tooltip("Harita lon ofseti (+=doğuya, derece).")]
    public float earthLongitudeOffsetDeg = 0f;

    // ===== Internal state =====
    float _latDeg, _lonDeg, _headingDeg, _speedKmh;
    bool _hasState;

    Vector3 _targetPosWS;
    Quaternion _targetRotWS;
    Quaternion _lastGoodRotWS;
    bool _hasLastGoodRot;

    void Start()
    {
        _lastGoodRotWS = transform.rotation;
        _hasLastGoodRot = true;

        if (earthWorldRadiusKm < 1f) earthWorldRadiusKm = 6371f;
        if (timeCompression <= 0f) timeCompression = 1f;
        if (posLerp < 0f) posLerp = 0f;
        if (rotLerp < 0f) rotLerp = 0f;
        if (maxStepDegPerFrame <= 0f) maxStepDegPerFrame = 0.7f;
    }

    // ===== Public API =====
    /// JSON/Poller burayı çağırır
    public void ApplyFlightState(float latDeg, float lonDeg, float headingDeg, float speedIn)
    {
        // Hızı km/h'ye çevir
        switch (inputSpeedUnit)
        {
            case SpeedUnit.Ms: _speedKmh = speedIn * 3.6f; break;
            case SpeedUnit.Knots: _speedKmh = speedIn * 1.852f; break;
            default: _speedKmh = speedIn; break;
        }

        // Heading'i ham olarak kaydet (düzeltmeyi render’da yapıyoruz)
        _headingDeg = headingDeg;

        // Hareket modlarında dış pozisyonu alma -> geri atmayı önler
        if (ignoreExternalPosWhileMoving && (useWaypoint || simulateMovement))
        {
            _hasState = true;
            return;
        }

        // Konumu direkt set et (gerekirse küçük yumuşatma eklenebilir)
        _latDeg = latDeg;
        _lonDeg = lonDeg;
        _hasState = true;
    }

    /// Waypoint kur
    public void SetWaypoint(float latDeg, float lonDeg)
    {
        waypointLatDeg = latDeg;
        waypointLonDeg = lonDeg;
        useWaypoint = true;
    }

    // ===== Update =====
    void LateUpdate()
    {
        if (!earth || !_hasState) return;

        // 1) İlerleme
        if (useWaypoint) StepTowardsWaypoint(Time.deltaTime);
        else if (simulateMovement && _speedKmh > 0f) StepOnSphere(Time.deltaTime);

        // 2) LOCAL: konum ve teğet çerçeve (alignment ile)
        Vector3 uL = LatLonUnitLocalAligned(_latDeg, _lonDeg);
        Vector3 eastL = EastUnitLocalAligned(_latDeg, _lonDeg);
        Vector3 northL = NorthUnitLocalAligned(_latDeg, _lonDeg);

        // 3) LOCAL -> WORLD
        Vector3 localPos = uL * (earthLocalRadius + iconLocalOffset);
        Vector3 posWS = earth.TransformPoint(localPos);
        Vector3 upWS = earth.TransformDirection(uL).normalized;
        Vector3 eastWS = earth.TransformDirection(eastL).normalized;
        Vector3 northWS = earth.TransformDirection(northL).normalized;

        // 4) Heading (0°=Kuzey, 90°=Doğu)
        float h = (ConvertHeadingIn(_headingDeg) + headingOffsetDeg) * Mathf.Deg2Rad;
        Vector3 fwdWS = Mathf.Sin(h) * eastWS + Mathf.Cos(h) * northWS;
        if (!IsFinite(fwdWS) || fwdWS.sqrMagnitude < 1e-10f)
        {
            fwdWS = Vector3.Cross(upWS, Vector3.right).normalized;
            if (fwdWS.sqrMagnitude < 1e-10f) fwdWS = Vector3.Cross(upWS, Vector3.forward).normalized;
        }

        // 5) Rotasyon
        Quaternion worldRot = SafeLookRotation(fwdWS, upWS, _hasLastGoodRot ? _lastGoodRotWS : Quaternion.identity);

        Vector3 mdlFwd = modelNoseLocal.sqrMagnitude < 1e-8f ? Vector3.forward : modelNoseLocal;
        Vector3 mdlUp = modelNormalLocal.sqrMagnitude < 1e-8f ? Vector3.up : modelNormalLocal;
        if (Vector3.Cross(mdlFwd.normalized, mdlUp.normalized).sqrMagnitude < 1e-6f)
            mdlUp = Mathf.Abs(mdlFwd.y) < 0.99f ? Vector3.up : Vector3.right;

        Quaternion modelBasis = SafeLookRotation(mdlFwd, mdlUp, Quaternion.identity);
        Quaternion rotWS = worldRot * Quaternion.Inverse(modelBasis);

        // 6) Yumuşat ve uygula
        _targetPosWS = posWS;
        _targetRotWS = rotWS;

        float pAlpha = 1f - Mathf.Exp(-posLerp * Time.deltaTime);
        float rAlpha = 1f - Mathf.Exp(-rotLerp * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, _targetPosWS, pAlpha);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotWS, rAlpha);

        if (IsFinite(transform.forward) && IsFinite(transform.up))
        {
            _lastGoodRotWS = transform.rotation;
            _hasLastGoodRot = true;
        }
    }

    // ===== Movement helpers =====
    void StepOnSphere(float dt)
    {
        Vector3 uL = LatLonUnitLocalAligned(_latDeg, _lonDeg);
        Vector3 eastL = EastUnitLocalAligned(_latDeg, _lonDeg);
        Vector3 northL = NorthUnitLocalAligned(_latDeg, _lonDeg);

        float h = ConvertHeadingIn(_headingDeg) * Mathf.Deg2Rad;
        Vector3 fwdL = Mathf.Sin(h) * eastL + Mathf.Cos(h) * northL;

        Vector3 axisL = Vector3.Cross(fwdL, uL);
        if (!IsFinite(axisL) || axisL.sqrMagnitude < 1e-10f)
            axisL = Vector3.Cross(Vector3.right, uL);
        axisL.Normalize();

        float stepDeg = (_speedKmh / 3600f) / Mathf.Max(1f, earthWorldRadiusKm)
                      * Mathf.Rad2Deg * dt * timeCompression;

        stepDeg = Mathf.Min(stepDeg, maxStepDegPerFrame);

        Vector3 uL2 = Quaternion.AngleAxis(stepDeg, axisL) * uL;
        WriteLatLonFromUnitAligned(uL2);
    }

    void StepTowardsWaypoint(float dt)
    {
        // 0,0 guard
        if (Mathf.Abs(waypointLatDeg) < 1e-6f && Mathf.Abs(waypointLonDeg) < 1e-6f) return;

        Vector3 u = LatLonUnitLocalAligned(_latDeg, _lonDeg);
        Vector3 v = LatLonUnitLocalAligned(waypointLatDeg, waypointLonDeg);

        float dot = Mathf.Clamp(Vector3.Dot(u, v), -1f, 1f);
        float angToGo = Mathf.Acos(dot) * Mathf.Rad2Deg;

        if (angToGo < arriveEpsilonDeg)
        {
            _latDeg = waypointLatDeg;
            _lonDeg = waypointLonDeg;
            useWaypoint = false;
            return;
        }

        Vector3 axis = Vector3.Cross(u, v);
        if (!IsFinite(axis) || axis.sqrMagnitude < 1e-10f)
            axis = Vector3.Cross(u, Mathf.Abs(u.y) < 0.99f ? Vector3.up : Vector3.right);
        axis.Normalize();

        float stepDeg = (_speedKmh / 3600f) / Mathf.Max(1f, earthWorldRadiusKm)
                      * Mathf.Rad2Deg * dt * timeCompression;

        stepDeg = Mathf.Min(stepDeg, angToGo, maxStepDegPerFrame);

        Vector3 u2 = Quaternion.AngleAxis(stepDeg, axis) * u;
        WriteLatLonFromUnitAligned(u2);

        // Heading'i güncelle (teğet)
        Vector3 t = Vector3.Cross(axis, u2).normalized;
        Vector3 east = EastUnitLocalAligned(_latDeg, _lonDeg);
        Vector3 north = NorthUnitLocalAligned(_latDeg, _lonDeg);
        float hdgRad = Mathf.Atan2(Vector3.Dot(t, east), Vector3.Dot(t, north));
        _headingDeg = hdgRad * Mathf.Rad2Deg;
    }

    // ===== Lat/Lon helpers (ALIGNED) =====
    Vector3 LatLonUnitLocalAligned(float latDeg, float lonDeg)
    {
        float s = northAtPositiveY ? 1f : -1f;  // N/S flip
        float lat = (s * latDeg + earthLatitudeOffsetDeg) * Mathf.Deg2Rad;
        float lon = (lonDeg + earthLongitudeOffsetDeg) * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon),
                           Mathf.Sin(lat),
                           Mathf.Cos(lat) * Mathf.Sin(lon)).normalized;
    }

    Vector3 EastUnitLocalAligned(float latDeg, float lonDeg)
    {
        float s = northAtPositiveY ? 1f : -1f;
        float lat = (s * latDeg + earthLatitudeOffsetDeg) * Mathf.Deg2Rad;
        float lon = (lonDeg + earthLongitudeOffsetDeg) * Mathf.Deg2Rad;

        Vector3 dLon = new Vector3(-Mathf.Cos(lat) * Mathf.Sin(lon), 0f, Mathf.Cos(lat) * Mathf.Cos(lon));
        return dLon.normalized;
    }

    Vector3 NorthUnitLocalAligned(float latDeg, float lonDeg)
    {
        float s = northAtPositiveY ? 1f : -1f;
        float lat = (s * latDeg + earthLatitudeOffsetDeg) * Mathf.Deg2Rad;
        float lon = (lonDeg + earthLongitudeOffsetDeg) * Mathf.Deg2Rad;

        Vector3 dLatPrime = new Vector3(-Mathf.Sin(lat) * Mathf.Cos(lon),
                                         Mathf.Cos(lat),
                                        -Mathf.Sin(lat) * Mathf.Sin(lon));
        return (s * dLatPrime).normalized; // coğrafi kuzeye eşlenmiş
    }

    void WriteLatLonFromUnitAligned(Vector3 uL)
    {
        // ofsetli/flip'li lat/lon'dan gerçek lat/lon'a geri çöz
        float latRawDeg = Mathf.Asin(Mathf.Clamp(uL.y, -1f, 1f)) * Mathf.Rad2Deg;
        float lonRawDeg = Mathf.Atan2(uL.z, uL.x) * Mathf.Rad2Deg;

        float s = northAtPositiveY ? 1f : -1f;
        _latDeg = (latRawDeg - earthLatitudeOffsetDeg) / s;
        _lonDeg = lonRawDeg - earthLongitudeOffsetDeg;
    }

    // ===== Utils =====
    static bool IsFinite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

    static Quaternion SafeLookRotation(Vector3 forward, Vector3 up, Quaternion fallback)
    {
        if (!IsFinite(up) || up.sqrMagnitude < 1e-10f) up = Vector3.up;
        if (!IsFinite(forward) || forward.sqrMagnitude < 1e-10f)
        {
            Vector3 east = Vector3.Cross(up, Vector3.right);
            if (east.sqrMagnitude < 1e-10f) east = Vector3.Cross(up, Vector3.forward);
            east = east.sqrMagnitude > 0f ? east.normalized : Vector3.right;

            Vector3 north = Vector3.Cross(up.normalized, east).normalized;
            forward = north.sqrMagnitude > 0f ? north : Vector3.forward;
            if (!IsFinite(forward) || forward.sqrMagnitude < 1e-10f) return fallback;
        }
        return Quaternion.LookRotation(forward.normalized, up.normalized);
    }

    static float _headingOffsetFix(float hdg)
    {
        hdg %= 360f; if (hdg < 0f) hdg += 360f; return hdg;
    }

    float ConvertHeadingIn(float hInDeg)
    {
        if (headingZeroAtEast) hInDeg = 90f - hInDeg; // math→nav
        if (headingCounterClockwise) hInDeg = -hInDeg;      // CCW→CW
        if (headingAdd180) hInDeg += 180f;
        return _headingOffsetFix(hInDeg);
    }
}