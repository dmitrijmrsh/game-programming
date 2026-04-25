using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CreatureSymbiosisVFX : MonoBehaviour
{
    [SerializeField] FlockingManager flockingManager;
    [SerializeField] Transform source;
    [SerializeField] float maxLinks = 6f;
    [SerializeField] float linkWidth = 0.06f;
    [SerializeField] float linkHeightOffset = 0.15f;

    readonly List<LineRenderer> activeLinks = new();
    ParticleSystem pulseParticles;

    public int ActiveLinkCount => activeLinks.Count;

    void Awake()
    {
        pulseParticles = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (source == null || flockingManager == null)
        {
            ClearLinks();
            return;
        }

        int linkIndex = 0;
        var agents = flockingManager.Agents;
        for (int i = 0; i < agents.Count; i++)
        {
            if (!agents[i].IsFollowing)
                continue;

            if (linkIndex >= Mathf.RoundToInt(maxLinks))
                break;

            var link = GetOrCreateLink(linkIndex, agents[i].GlowColor);
            UpdateLink(link, agents[i]);
            linkIndex++;
        }

        while (activeLinks.Count > linkIndex)
        {
            DestroyImmediate(activeLinks[activeLinks.Count - 1].gameObject);
            activeLinks.RemoveAt(activeLinks.Count - 1);
        }

        if (pulseParticles != null)
        {
            if (linkIndex > 0 && !pulseParticles.isPlaying)
                pulseParticles.Play();
            else if (linkIndex == 0 && pulseParticles.isPlaying)
                pulseParticles.Stop();
        }
    }

    void ClearLinks()
    {
        for (int i = activeLinks.Count - 1; i >= 0; i--)
        {
            if (activeLinks[i] != null)
                DestroyImmediate(activeLinks[i].gameObject);
        }

        activeLinks.Clear();
        if (pulseParticles != null && pulseParticles.isPlaying)
            pulseParticles.Stop();
    }

    LineRenderer GetOrCreateLink(int index, Color color)
    {
        if (index < activeLinks.Count && activeLinks[index] != null)
            return activeLinks[index];

        var go = new GameObject($"SymbiosisLink_{index}");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = CreateLinkMaterial(color);
        activeLinks.Add(line);
        return line;
    }

    void UpdateLink(LineRenderer line, FlockingAgent agent)
    {
        Vector3 origin = source.position + source.up * linkHeightOffset;
        Vector3 target = agent.transform.position;
        float pulse = 0.5f + Mathf.Sin(Time.time * 5f + activeLinks.IndexOf(line)) * 0.15f;

        line.startWidth = linkWidth * (1.1f + pulse);
        line.endWidth = linkWidth * 0.55f;
        line.startColor = new Color(agent.GlowColor.r, agent.GlowColor.g, agent.GlowColor.b, 0.7f);
        line.endColor = new Color(agent.GlowColor.r, agent.GlowColor.g, agent.GlowColor.b, 0.08f);
        line.SetPosition(0, origin);
        line.SetPosition(1, target);
    }

    static Material CreateLinkMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        return material;
    }
}
