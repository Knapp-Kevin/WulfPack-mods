using System;

namespace WulfPack.VidarShrugged;

internal sealed class FrameMetrics
{
    private readonly float[] _samples;
    private int _count;
    private int _writeIndex;

    public FrameMetrics(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _samples = new float[capacity];
    }

    public void Add(float frameMilliseconds)
    {
        if (frameMilliseconds <= 0f || float.IsNaN(frameMilliseconds) || float.IsInfinity(frameMilliseconds))
        {
            return;
        }

        _samples[_writeIndex] = frameMilliseconds;
        _writeIndex = (_writeIndex + 1) % _samples.Length;
        if (_count < _samples.Length)
        {
            _count++;
        }
    }

    public FrameMetricsSnapshot SnapshotAndReset()
    {
        if (_count == 0)
        {
            return FrameMetricsSnapshot.Empty;
        }

        float[] ordered = new float[_count];
        Array.Copy(_samples, ordered, _count);
        Array.Sort(ordered);

        double total = 0d;
        for (int i = 0; i < ordered.Length; i++)
        {
            total += ordered[i];
        }

        float averageMs = (float)(total / ordered.Length);
        FrameMetricsSnapshot snapshot = new(
            ordered.Length,
            averageMs,
            averageMs > 0f ? 1000f / averageMs : 0f,
            Percentile(ordered, 0.50),
            Percentile(ordered, 0.95),
            Percentile(ordered, 0.99),
            Percentile(ordered, 0.999),
            ordered[ordered.Length - 1]);

        Reset();
        return snapshot;
    }

    public void Reset()
    {
        _count = 0;
        _writeIndex = 0;
    }

    private static float Percentile(float[] ordered, double percentile)
    {
        if (ordered.Length == 0)
        {
            return 0f;
        }

        int index = (int)Math.Ceiling(percentile * ordered.Length) - 1;
        index = Math.Max(0, Math.Min(index, ordered.Length - 1));
        return ordered[index];
    }
}

internal readonly struct FrameMetricsSnapshot
{
    public static FrameMetricsSnapshot Empty { get; } = new(0, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

    public FrameMetricsSnapshot(
        int sampleCount,
        float averageMs,
        float averageFps,
        float p50Ms,
        float p95Ms,
        float p99Ms,
        float p999Ms,
        float maxMs)
    {
        SampleCount = sampleCount;
        AverageMs = averageMs;
        AverageFps = averageFps;
        P50Ms = p50Ms;
        P95Ms = p95Ms;
        P99Ms = p99Ms;
        P999Ms = p999Ms;
        MaxMs = maxMs;
    }

    public int SampleCount { get; }
    public float AverageMs { get; }
    public float AverageFps { get; }
    public float P50Ms { get; }
    public float P95Ms { get; }
    public float P99Ms { get; }
    public float P999Ms { get; }
    public float MaxMs { get; }
}
