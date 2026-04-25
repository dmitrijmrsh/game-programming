using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Victory, GameOver }

    [SerializeField] int totalCrystals = 13;
    [SerializeField] HullIntegrity hullIntegrity;

    int collectedCrystals;
    GameState state = GameState.Playing;
    float playTime;

    static GameManager instance;
    public static GameManager Instance => instance;

    public GameState State => state;
    public int CollectedCrystals => collectedCrystals;
    public int TotalCrystals => totalCrystals;
    public float PlayTime => playTime;

    public event Action<int, int> OnCrystalCollected;
    public event Action OnVictory;
    public event Action OnGameOver;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (hullIntegrity != null)
            hullIntegrity.OnHullDestroyed += HandleHullDestroyed;

        var crystals = FindObjectsByType<EchoCrystal>(FindObjectsSortMode.None);
        if (crystals.Length > 0)
            totalCrystals = crystals.Length;
    }

    void Update()
    {
        if (state == GameState.Playing)
            playTime += Time.deltaTime;
    }

    public void CollectCrystal()
    {
        if (state != GameState.Playing) return;

        collectedCrystals++;
        OnCrystalCollected?.Invoke(collectedCrystals, totalCrystals);

        if (collectedCrystals >= totalCrystals)
        {
            state = GameState.Victory;
            OnVictory?.Invoke();
        }
    }

    void HandleHullDestroyed()
    {
        if (state != GameState.Playing) return;
        state = GameState.GameOver;
        OnGameOver?.Invoke();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        if (hullIntegrity != null)
            hullIntegrity.OnHullDestroyed -= HandleHullDestroyed;
    }
}
