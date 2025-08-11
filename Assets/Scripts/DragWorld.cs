using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

public class DragWorld : MonoBehaviour
{

    [Header("Settings")]
    public float sensivity = 150f;
    public float yClamp = 85f;
    public bool invertY = true;

    [Header("Refs")]
    public Transform yawNode;  
    public Transform pitchNode; 
    public Transform earth;       
    public Transform clouds;       
    public Camera cam;

    float camDistance;
    Vector3 zoomTarget;

    [Header("Zoom")]
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 0.02f;
    public bool invertZoom = false;

    Transform self;
    bool dragging;
    Quaternion startRotation;

    float yawY;
    float pitchX;

    void Start()
    {
        self = transform;


        yawY = yawNode.eulerAngles.y;
        float x = pitchNode.localEulerAngles.x;
        pitchX = (x > 180f) ? x - 360f : x;

        //Camera
        // hedef merkez
        zoomTarget = pitchNode ? pitchNode.position : earth.position;

        // g�venli min mesafe hesab� (Earth/Clouds hangisi b�y�kse onu al)
        float rEarth = GetApproxRadius(earth);
        float rCloud = GetApproxRadius(clouds);
        float targetRadius = Mathf.Max(rEarth, rCloud);

        // near clip + k���k pay ekle
        float safeMin = targetRadius + cam.nearClipPlane + 0.3f;
        minDistance = Mathf.Max(minDistance, safeMin);

        // ba�lang�� uzakl���n� g�venli aral��a s�k��t�r
        camDistance = Mathf.Clamp(Vector3.Distance(cam.transform.position, zoomTarget), minDistance, maxDistance);
    }

    private float GetApproxRadius(Transform clouds)
    {
        if (!clouds) return 0.5f;
        // �nce collider'a bak (en do�ru)
        if (clouds.TryGetComponent<SphereCollider>(out var sc))
            return sc.radius * clouds.lossyScale.x; // uniform scale varsay�ld�
                                                    // Yoksa renderer bounds (yakla��k)
        if (clouds.TryGetComponent<Renderer>(out var rend))
            return rend.bounds.extents.magnitude; // yar� �ap yerine yar� diyagonal; g�venli olsun
        return 0.5f;
    }

        void Update()
    {

        MouseScroll();

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(PointerId.mousePointerId))
            return;


        //Allowing to put world stable when we click ui elements(Button, Slider ...)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;


        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit)) { 

                if (hit.transform == earth || hit.transform.IsChildOf(earth) ||
                    hit.transform == clouds || hit.transform.IsChildOf(clouds))
                    dragging = true;
            }

        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
        }
        if (!dragging)
        {
            return;
        }

        Vector2 d = Mouse.current.delta.ReadValue();

        float mx = d.x * sensivity * Time.deltaTime;
        float my = d.y * (invertY ? 1f : -1f) * sensivity * Time.deltaTime;

        yawY += -mx;
        pitchX = Mathf.Clamp(pitchX + (my), -yClamp, yClamp);



        //Quaternion, euler angles again converted to Quaternion values (numbers). Note: Z-axis is 0 since we don't want to move it
        var yawRot = Quaternion.AngleAxis(yawY, Vector3.up);
        yawNode.rotation = yawRot;

        // 2) Pitch ekseni: KAMERANIN right'� (ekran yatay�)
        Vector3 pitchAxisWorld = cam.transform.right;

        // 3) Pitch'i bu eksende world'te uygula ve yaw'a ekle
        pitchNode.rotation = Quaternion.AngleAxis(pitchX, pitchAxisWorld) * yawRot;


    }
    public void MouseScroll()
    {
        if (Mouse.current == null || cam == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) < 0.01f) return;

        //Refreshing per frame to get accurate pitch 
        zoomTarget = pitchNode ? pitchNode.position : earth.position;

        //Distance vector between camera and target
        Vector3 v = cam.transform.position - zoomTarget;
        //Magnitude of that vector
        float d = v.magnitude;
        Vector3 dir = v/d;

        float sign = invertZoom ? -1f : 1f;
        float newD = Mathf.Clamp(d - sign * scrollY * zoomSpeed, minDistance, maxDistance);

        cam.transform.position = zoomTarget + dir * newD;
        cam.transform.LookAt(zoomTarget, Vector3.up);
    }

}

