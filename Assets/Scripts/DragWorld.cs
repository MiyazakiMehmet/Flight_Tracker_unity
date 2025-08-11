using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class DragWorld : MonoBehaviour
{

    [Header("Settings")]
    public float sensivity = 150f;
    public float yClamp = 85f;
    public bool invertY = true;

    [Header("Refs")]
    public Transform yawNode;   // Root'un child'ý
    public Transform pitchNode; // yawNode'un child'ý
    public Transform earth;        // sphere mesh + collider
    public Transform clouds;       // sphere mesh (biraz büyük)
    public Camera cam;

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
    }

    void Update()
    {
        //Allowing to put world stable when we click ui elements(Button, Slider ...)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && (hit.transform == self || hit.transform.IsChildOf(self))) {

                // Earth veya Clouds’a týklanýrsa drag baþlasýn
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

        // 2) Pitch ekseni: KAMERANIN right'ý (ekran yatayý)
        Vector3 pitchAxisWorld = cam.transform.right;

        // 3) Pitch'i bu eksende world'te uygula ve yaw'a ekle
        pitchNode.rotation = Quaternion.AngleAxis(pitchX, pitchAxisWorld) * yawRot;

    }
}
