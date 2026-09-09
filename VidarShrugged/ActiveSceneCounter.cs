using System.Diagnostics;
using UnityEngine;

namespace WulfPack.VidarShrugged;

internal static class ActiveSceneCounter
{
    public static ActiveSceneSnapshot Capture()
    {
        long start = Stopwatch.GetTimestamp();
        Component[] components = Resources.FindObjectsOfTypeAll<Component>();

        int renderers = 0;
        int lights = 0;
        int particleSystems = 0;
        int audioSources = 0;
        int colliders = 0;
        int rigidbodies = 0;
        int monoBehaviours = 0;

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            GameObject gameObject = component.gameObject;
            if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.activeInHierarchy)
            {
                continue;
            }

            if (component is Renderer) renderers++;
            if (component is Light) lights++;
            if (component is ParticleSystem) particleSystems++;
            if (component is AudioSource) audioSources++;
            if (component is Collider) colliders++;
            if (component is Rigidbody) rigidbodies++;
            if (component is MonoBehaviour) monoBehaviours++;
        }

        double elapsedMilliseconds =
            (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;

        return new ActiveSceneSnapshot(
            renderers,
            lights,
            particleSystems,
            audioSources,
            colliders,
            rigidbodies,
            monoBehaviours,
            elapsedMilliseconds);
    }
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
        double elapsedMilliseconds)
    {
        Renderers = renderers;
        Lights = lights;
        ParticleSystems = particleSystems;
        AudioSources = audioSources;
        Colliders = colliders;
        Rigidbodies = rigidbodies;
        MonoBehaviours = monoBehaviours;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public int Renderers { get; }
    public int Lights { get; }
    public int ParticleSystems { get; }
    public int AudioSources { get; }
    public int Colliders { get; }
    public int Rigidbodies { get; }
    public int MonoBehaviours { get; }
    public double ElapsedMilliseconds { get; }
}
