using UnityEngine;

public class ScoutHighlight : MonoBehaviour
{
    [SerializeField] CreatureBuff creatureBuff;
    [SerializeField] Transform player;
    [SerializeField] GameObject markerPrefab;

    readonly System.Collections.Generic.List<GameObject> markers = new();

    void Update()
    {
        if (creatureBuff == null || player == null) return;

        foreach (var m in markers)
            if (m != null) Destroy(m);
        markers.Clear();

        if (!creatureBuff.ScoutActive) return;

        var crystals = FindObjectsByType<EchoCrystal>(FindObjectsSortMode.None);
        foreach (var crystal in crystals)
        {
            if (crystal.Collected) continue;
            float dist = Vector3.Distance(player.position, crystal.transform.position);
            if (dist > creatureBuff.ScoutRange) continue;

            var light = crystal.GetComponentInChildren<Light>();
            if (light != null)
                light.range = 30f;
        }
    }
}
