using UnityEngine;

public class SunDirectionUpdater : MonoBehaviour
{
    public Light sun;
    public Material earthMaterial;

    void Update()
    {
        Vector3 sunDir = -sun.transform.forward;        
        earthMaterial.SetVector("_SunDir", sunDir);
    }
}
