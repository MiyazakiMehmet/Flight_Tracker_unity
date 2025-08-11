using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

public class SunDirectionUpdater : MonoBehaviour
{
    public Light sun;
    public Material earthMaterial;
    public Material cloudMaterial; // bulutlar için ekledik

    void Update()
    {
        // Directional Light’ýn baktýðý yön (eksi forward)
        Vector3 sunDir = -sun.transform.forward;
        sunDir.Normalize();

        // Hem Earth hem Clouds shader’ýna gönder
        earthMaterial.SetVector("_SunDir", sunDir);
        cloudMaterial.SetVector("_SunDir", sunDir);
    }
}
