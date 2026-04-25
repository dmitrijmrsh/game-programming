using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
    [SerializeField] FlockingManager flockingManager;

    void Start()
    {
        if (flockingManager == null)
            flockingManager = FindFirstObjectByType<FlockingManager>();

        foreach (var agent in FindObjectsByType<FlockingAgent>(FindObjectsSortMode.None))
        {
            if (flockingManager != null)
                flockingManager.Register(agent);
        }
    }
}
