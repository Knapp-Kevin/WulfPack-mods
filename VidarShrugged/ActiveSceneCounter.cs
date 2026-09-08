using UnityEngine;

namespace WulfPack.VidarShrugged;

internal static class ActiveSceneCounter
{
    public static ActiveSceneSnapshot Capture()
    {
        return new ActiveSceneSnapshot(
            CountActive<Renderer>(),
            CountActive<Light>(),
            CountActive<ParticleSystem>(),
            CountActive<AudioSource>(),
            CountActive<Collider>(),
            CountActive<Rigidbody>(),
            CountActive<MonoBehaviour>());
    }

    private static int CountActive<T>() where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        int count = 0;

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null)
            {
                continue;
            }

            GameObject gameObject = component.gameObject;
            if (gameObject != null && gameObject.scene.IsValid() && gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
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
        int monoBehaviours)
    {
        Renderers = renderers;
        Lights = lights;
        ParticleSystems = particleSystems;
        AudioSources = audioSources;
        Colliders = colliders;
        Rigidbodies = rigidbodies;
        MonoBehaviours = monoBehaviours;
    }

    public int Renderers { get; }
    public int Lights { get; }
    public int ParticleSystems { get; }
    public int AudioSources { get; }
    public int Colliders { get; }
    public int Rigidbodies { get; }
    public int MonoBehaviours { get; }
}
