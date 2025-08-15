using UnityEngine;
using UnityEngine.SceneManagement;

public class FlightManager : MonoBehaviour
{
    public Transform worldParent;      // World
    public Transform earth;            // World
    public GameObject airplanePrefab;
    public FlightDataGetter dataGetter;
    public bool autoSpawnIfMissing = true;

    private PlainTrack _plane;

    void Start()
    {
        if (dataGetter == null) { Debug.LogError("[FM] dataGetter yok."); return; }
        if (worldParent == null) { Debug.LogError("[FM] worldParent (World) atanmamýþ."); return; }
        if (earth == null) earth = worldParent;

        // Zaten atanmýþ uçak varsa onu sahneye/parent'a zorla yerleþtir
        if (dataGetter.airplane != null)
        {
            _plane = dataGetter.airplane;
            ForceAttachToWorld(_plane.gameObject);
            FinalizePlane(_plane);
            Debug.Log("[FM] Var olan uçaðý baðladým ve World altýna aldým.");
            return;
        }

        // Gerekirse yeni spawn et
        if (autoSpawnIfMissing)
        {
            if (airplanePrefab == null) { Debug.LogError("[FM] airplanePrefab yok."); return; }

            // 1) Instantiate parent ile
            GameObject go = Instantiate(airplanePrefab, worldParent, false);

            // 2) Her ihtimale karþý tekrar parent’la (baþka script müdahelesine karþý)
            go.transform.SetParent(worldParent, false);

            // 3) DDOL’a taþýndýysa ana sahneye geri getir
            ForceAttachToWorld(go);

            _plane = go.GetComponent<PlainTrack>();
            if (_plane == null) { Debug.LogError("[FM] Prefab’ta PlainTrack yok!"); return; }

            FinalizePlane(_plane);
            dataGetter.airplane = _plane;

            Debug.Log($"[FM] Spawn OK -> parent: {go.transform.parent?.name}, scene: {go.scene.name}");
        }
        else
        {
            Debug.LogWarning("[FM] autoSpawnIfMissing=false ve dataGetter.airplane yok.");
        }
    }

    void FinalizePlane(PlainTrack pt)
    {
        if (pt.earth == null) pt.earth = earth; // World
    }

    void ForceAttachToWorld(GameObject go)
    {
        // DDOL sahnesindeyse ana sahneye geri taþý
        if (go.scene.name == "DontDestroyOnLoad")
        {
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        }
        // Son kez parent’ý garanti et
        go.transform.SetParent(worldParent, false);
    }
}
