using UnityEngine;
using System.Collections.Generic;

public class FlockingManager : MonoBehaviour
{
    [Header("Flocking Weights")]
    [SerializeField] float separationWeight = 1.5f;
    [SerializeField] float alignmentWeight = 1.0f;
    [SerializeField] float cohesionWeight = 1.0f;
    [SerializeField] float attractionWeight = 8f;
    [SerializeField] float wanderWeight = 0.5f;

    [Header("Distances")]
    [SerializeField] float neighborRadius = 10f;
    [SerializeField] float separationRadius = 3f;
    [SerializeField] float followDistance = 20f;
    [SerializeField] float attractionRange = 50f;
    [SerializeField] float attractionAngle = 70f;

    [Header("Current Influence")]
    [SerializeField] float currentInfluence = 0.3f;

    readonly List<FlockingAgent> agents = new();
    Transform attractTarget;
    bool attractionActive;

    public IReadOnlyList<FlockingAgent> Agents => agents;

    public void Register(FlockingAgent agent) => agents.Add(agent);
    public void Unregister(FlockingAgent agent) => agents.Remove(agent);
    public float AttractionRange { get => attractionRange; set => attractionRange = Mathf.Max(0f, value); }
    public float AttractionAngle { get => attractionAngle; set => attractionAngle = Mathf.Clamp(value, 1f, 180f); }

    public void SetAttractTarget(Transform target, bool active)
    {
        attractTarget = target;
        attractionActive = active;
    }

    public int CountFollowing()
    {
        int count = 0;
        for (int i = 0; i < agents.Count; i++)
            if (agents[i].IsFollowing) count++;
        return count;
    }

    public int CountFollowingOfType(CreatureType type)
    {
        int count = 0;
        for (int i = 0; i < agents.Count; i++)
            if (agents[i].IsFollowing && agents[i].Type == type) count++;
        return count;
    }

    void Update()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            Vector3 sep = Vector3.zero, ali = Vector3.zero, coh = Vector3.zero;
            int neighborCount = 0;

            for (int j = 0; j < agents.Count; j++)
            {
                if (i == j) continue;
                Vector3 diff = agent.transform.position - agents[j].transform.position;
                float dist = diff.magnitude;

                if (dist < neighborRadius)
                {
                    if (dist < separationRadius && dist > 0.01f)
                        sep += diff / (dist * dist);

                    ali += agents[j].Velocity;
                    coh += agents[j].transform.position;
                    neighborCount++;
                }
            }

            Vector3 steer = Vector3.zero;

            if (neighborCount > 0)
            {
                ali /= neighborCount;
                coh = (coh / neighborCount - agent.transform.position);

                steer += sep.normalized * separationWeight;
                steer += ali.normalized * alignmentWeight;
                steer += coh.normalized * cohesionWeight;
            }

            if (attractionActive && attractTarget != null)
            {
                Vector3 toTarget = attractTarget.position - agent.transform.position;
                float dist = toTarget.magnitude;
                bool inBeam = IsInsideAttractionCone(agent.transform.position);

                if (inBeam && dist > followDistance * 0.4f)
                    steer += toTarget.normalized * attractionWeight;
                else if (inBeam)
                    steer += -toTarget.normalized * separationWeight * 0.3f;
                else
                    steer += agent.ComputeWander() * wanderWeight;

                agent.IsFollowing = inBeam && dist < followDistance * 2.5f;
                agent.SetFollowIntensity(agent.IsFollowing ? 1f - dist / (followDistance * 2.5f) : 0f);
            }
            else
            {
                steer += agent.ComputeWander() * wanderWeight;
                if (agent.IsFollowing)
                {
                    agent.IsFollowing = false;
                    agent.SetFollowIntensity(0f);
                }
            }

            if (CurrentField.Instance != null)
            {
                Vector3 current = CurrentField.Instance.SampleCurrent(agent.transform.position, Time.time);
                steer += current * currentInfluence;
            }

            agent.Velocity += steer * Time.deltaTime;
            agent.TickHabitatConstraint(Time.deltaTime);
            agent.ApplyVelocity();
        }
    }

    bool IsInsideAttractionCone(Vector3 worldPosition)
    {
        if (!attractionActive || attractTarget == null)
            return false;

        Vector3 offset = worldPosition - attractTarget.position;
        float distance = offset.magnitude;
        if (distance > attractionRange)
            return false;

        if (distance <= 0.001f)
            return true;

        float angle = Vector3.Angle(attractTarget.forward, offset);
        return angle <= attractionAngle * 0.5f;
    }
}
