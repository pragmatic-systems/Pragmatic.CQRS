using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Pragmatic.CQRS.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run(typeof(Program).Assembly, args: args);

        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
    }
}
