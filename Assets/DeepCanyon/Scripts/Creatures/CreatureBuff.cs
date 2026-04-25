using UnityEngine;

public class CreatureBuff : MonoBehaviour
{
    [SerializeField] FlockingManager flockingManager;
    [SerializeField] BathyscapheController bathyscaphe;
    [SerializeField] HullIntegrity hullIntegrity;
    [SerializeField] int minCreaturesForBuff = 1;
    [SerializeField] float buffGraceDuration = 6f;

    [Header("Buff Values")]
    [SerializeField] float healRate = 2f;
    [SerializeField] float speedBoost = 1.3f;
    [SerializeField] float armorReduction = 0.5f;
    [SerializeField] float scoutRange = 50f;

    bool healerActive, speederActive, armorerActive, scoutActive;
    float healerTimer, speederTimer, armorerTimer, scoutTimer;

    public bool HealerActive => healerActive;
    public bool SpeederActive => speederActive;
    public bool ArmorerActive => armorerActive;
    public bool ScoutActive => scoutActive;
    public float ScoutRange => scoutRange;

    void Update()
    {
        RefreshBuffTimers();
        TickBuffTimers(Time.deltaTime);
        ApplyBuffEffects();
    }

    void RefreshBuffTimers()
    {
        if (flockingManager == null)
            return;

        if (flockingManager.CountFollowingOfType(CreatureType.Healer) >= minCreaturesForBuff)
            healerTimer = buffGraceDuration;

        if (flockingManager.CountFollowingOfType(CreatureType.Speeder) >= minCreaturesForBuff)
            speederTimer = buffGraceDuration;

        if (flockingManager.CountFollowingOfType(CreatureType.Armorer) >= minCreaturesForBuff)
            armorerTimer = buffGraceDuration;

        if (flockingManager.CountFollowingOfType(CreatureType.Scout) >= minCreaturesForBuff)
            scoutTimer = buffGraceDuration;
    }

    void TickBuffTimers(float deltaTime)
    {
        healerTimer = Mathf.Max(0f, healerTimer - deltaTime);
        speederTimer = Mathf.Max(0f, speederTimer - deltaTime);
        armorerTimer = Mathf.Max(0f, armorerTimer - deltaTime);
        scoutTimer = Mathf.Max(0f, scoutTimer - deltaTime);

        healerActive = healerTimer > 0f;
        speederActive = speederTimer > 0f;
        armorerActive = armorerTimer > 0f;
        scoutActive = scoutTimer > 0f;
    }

    void ApplyBuffEffects()
    {
        if (bathyscaphe != null)
            bathyscaphe.SpeedMultiplier = speederActive ? speedBoost : 1f;

        if (hullIntegrity != null)
        {
            hullIntegrity.ArmorMultiplier = armorerActive ? armorReduction : 1f;

            if (healerActive)
                hullIntegrity.Repair(healRate * Time.deltaTime);
        }
    }
}
