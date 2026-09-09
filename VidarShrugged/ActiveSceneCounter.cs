using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace WulfPack.VidarShrugged;

/// <summary>
/// Counts what is actually live in the loaded scene, and what the renderers are made of.
/// </summary>
/// <remarks>
/// Gate 0 measured this scan at 178-225 ms in a built-up world — eleven to thirteen whole
/// frames at 60 fps — and its cost landed in the p99.9 and max columns of the report
/// printed beside it. The instrument was measuring itself.
///
/// The cost came from <c>Resources.FindObjectsOfTypeAll&lt;Component&gt;()</c>, which
/// materialises an array of every Component held in memory — assets and inactive objects
/// included, not just the loaded scene — and then ran seven type tests per element in
/// managed code.
///
/// <see cref="Object.FindObjectsByType{T}(FindObjectsInactive, FindObjectsSortMode)"/>
/// searches loaded scenes only and filters by type and active state natively, so each
/// category is asked for directly. <c>FindObjectsInactive.Exclude</c> reproduces the old
/// <c>activeInHierarchy</c> filter and scene-only search reproduces the old
/// <c>scene.IsValid()</c> filter, so the counts mean what they meant before.
///
/// Gate 0 also found that a bare renderer count says where to look but not what to do, so
/// renderers are now grouped by prefab. "12,274 renderers" is a number; "4,000 of them are
/// wood_wall" is a lead.
/// </remarks>
internal static class ActiveSceneCounter
{
    private const FindObjectsInactive Active = FindObjectsInactive.Exclude;
    private const FindObjectsSortMode Unsorted = FindObjectsSortMode.None;

    public static ActiveSceneSnapshot Capture(int topPrefabs)
    {
        long start = Stopwatch.GetTimestamp();

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(Active, Unsorted);
        int lights = Object.FindObjectsByType<Light>(Active, Unsorted).Length;
        int particles = Object.FindObjectsByType<ParticleSystem>(Active, Unsorted).Length;
        int audio = Object.FindObjectsByType<AudioSource>(Active, Unsorted).Length;
        int colliders = Object.FindObjectsByType<Collider>(Active, Unsorted).Length;
        int bodies = Object.FindObjectsByType<Rigidbody>(Active, Unsorted).Length;
        int behaviours = Object.FindObjectsByType<MonoBehaviour>(Active, Unsorted).Length;

        IReadOnlyList<PrefabCount> topRenderers = TopRendererPrefabs(renderers, topPrefabs);

        double elapsedMilliseconds =
            (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;

        return new ActiveSceneSnapshot(
            renderers.Length, lights, particles, audio, colliders, bodies, behaviours,
            topRenderers, elapsedMilliseconds);
    }

    /// <summary>
    /// Groups renderers by the prefab they came from, heaviest first.
    /// </summary>
    /// <remarks>
    /// Unity appends "(Clone)" to instantiated objects, so it is trimmed to fold every
    /// instance of a prefab into one row. This is the difference between a count and a
    /// lead: it names which build pieces actually dominate a settlement.
    /// </remarks>
    private static IReadOnlyList<PrefabCount> TopRendererPrefabs(Renderer[] renderers, int top)
    {
        if (top <= 0 || renderers.Length == 0)
        {
            return System.Array.Empty<PrefabCount>();
        }

        Dictionary<string, int> counts = new(256);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string name = PrefabName(renderer.transform);
            counts.TryGetValue(name, out int current);
            counts[name] = current + 1;
        }

        List<PrefabCount> ordered = new(counts.Count);
        foreach (KeyValuePair<string, int> pair in counts)
        {
            ordered.Add(new PrefabCount(pair.Key, pair.Value));
        }

        ordered.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        if (ordered.Count > top)
        {
            ordered.RemoveRange(top, ordered.Count - top);
        }

        return ordered;
    }

    /// <summary>
    /// The owning prefab's name. Valheim nests renderers under a prefab root, so the
    /// topmost transform is what identifies the build piece rather than the mesh part.
    /// </summary>
    private static string PrefabName(Transform transform)
    {
        Transform root = transform.root != null ? transform.root : transform;
        string name = root.name;
        int clone = name.IndexOf("(Clone)", System.StringComparison.Ordinal);
        return clone >= 0 ? name.Substring(0, clone) : name;
    }
}

internal readonly struct PrefabCount
{
    public PrefabCount(string name, int count)
    {
        Name = name;
        Count = count;
    }

    public string Name { get; }
    public int Count { get; }
}

internal readonly struct ActiveSceneSnapshot
{
    public ActiveSceneSnapshot(
        int renderers,
        int lights,
        int particleSystems,
        int audioSources,
        int colliders,
        int rigidbodies,
        int monoBehaviours,
        IReadOnlyList<PrefabCount> topRendererPrefabs,
        double elapsedMilliseconds)
    {
        Renderers = renderers;
        Lights = lights;
        ParticleSystems = particleSystems;
        AudioSources = audioSources;
        Colliders = colliders;
        Rigidbodies = rigidbodies;
        MonoBehaviours = monoBehaviours;
        TopRendererPrefabs = topRendererPrefabs;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public int Renderers { get; }
    public int Lights { get; }
    public int ParticleSystems { get; }
    public int AudioSources { get; }
    public int Colliders { get; }
    public int Rigidbodies { get; }
    public int MonoBehaviours { get; }
    public IReadOnlyList<PrefabCount> TopRendererPrefabs { get; }
    public double ElapsedMilliseconds { get; }
}
