using UnityEngine;


public class SunDirectionUpdater : MonoBehaviour
{
    public Light sun;
    public Transform earth;           
    public Transform clouds;
    public Material earthMaterial;
    public Material cloudMaterial;

    public bool worldFixedSun = true;    // sarý çizgide sabit mod
    public Vector3 worldSunDir = new Vector3(1, 0, 0); // sarý çizginin yönü (dilediðin eksene ayarla)

    void LateUpdate()
    {
        Vector3 Lws = worldFixedSun
                    ? worldSunDir.normalized                  // Earth dönse de sabit
                    : (-sun.transform.forward).normalized;    // Earth’le birlikte dönsün istiyorsan

        Vector4 dir4 = new Vector4(Lws.x, Lws.y, Lws.z, 0);
        earthMaterial.SetVector("_SunDir", dir4);
        cloudMaterial.SetVector("_SunDir", dir4);

    }
}
