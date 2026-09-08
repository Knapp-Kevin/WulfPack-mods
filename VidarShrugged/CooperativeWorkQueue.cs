using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WulfPack.VidarShrugged;

internal interface IFrameBudgetWorkItem
{
    /// <summary>
    /// Execute one bounded slice of work.
    /// </summary>
    /// <returns>True when the work item is complete; otherwise false.</returns>
    bool ExecuteSlice();
}

internal sealed class CooperativeWorkQueue
{
    private readonly Queue<IFrameBudgetWorkItem> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(IFrameBudgetWorkItem workItem)
    {
        if (workItem is null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        _queue.Enqueue(workItem);
    }

    public int RunBudget(double maxMilliseconds)
    {
        if (maxMilliseconds <= 0d || _queue.Count == 0)
        {
            return 0;
        }

        long start = Stopwatch.GetTimestamp();
        double ticksPerMillisecond = Stopwatch.Frequency / 1000d;
        int completed = 0;

        while (_queue.Count > 0)
        {
            IFrameBudgetWorkItem current = _queue.Peek();
            if (current.ExecuteSlice())
            {
                _queue.Dequeue();
                completed++;
            }

            double elapsedMilliseconds = (Stopwatch.GetTimestamp() - start) / ticksPerMillisecond;
            if (elapsedMilliseconds >= maxMilliseconds)
            {
                break;
            }
        }

        return completed;
    }

    public void Clear()
    {
        _queue.Clear();
    }
}
