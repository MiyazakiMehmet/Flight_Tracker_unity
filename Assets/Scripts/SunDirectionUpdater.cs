using UnityEngine;


public class SunDirectionUpdater : MonoBehaviour
{
    public Light sun;
    public Transform earth;           
    public Transform clouds;
    public Material earthMaterial;
    public Material cloudMaterial;

    public bool worldFixedSun = true;    // sar� �izgide sabit mod
    public Vector3 worldSunDir = new Vector3(1, 0, 0); // sar� �izginin y�n� (diledi�in eksene ayarla)

    void LateUpdate()
    {
        Vector3 Lws = worldFixedSun
                    ? worldSunDir.normalized                  // Earth d�nse de sabit
                    : (-sun.transform.forward).normalized;    // Earth�le birlikte d�ns�n istiyorsan

        Vector4 dir4 = new Vector4(Lws.x, Lws.y, Lws.z, 0);
        earthMaterial.SetVector("_SunDir", dir4);
        cloudMaterial.SetVector("_SunDir", dir4);

    }
}
