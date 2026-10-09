using System.Diagnostics;

namespace AiLearning.Console.Services;

public sealed class PerformanceTimer : IDisposable
{
    private readonly string _operation;
    private readonly Stopwatch _stopwatch;

    public PerformanceTimer(string operation)
    {
        _operation = operation;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();

        System.Console.WriteLine(
            $"[PERF] {_operation}: {_stopwatch.ElapsedMilliseconds:N0} ms");
    }
}