using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

public class SunDirectionUpdater : MonoBehaviour
{
    public Light sun;
    public Material earthMaterial;
    public Material cloudMaterial; // bulutlar i�in ekledik

    void Update()
    {
        // Directional Light��n bakt��� y�n (eksi forward)
        Vector3 sunDir = -sun.transform.forward;
        sunDir.Normalize();

        // Hem Earth hem Clouds shader��na g�nder
        earthMaterial.SetVector("_SunDir", sunDir);
        cloudMaterial.SetVector("_SunDir", sunDir);
    }
}
