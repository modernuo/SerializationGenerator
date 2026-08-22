using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(ModernUO.Serialization.Generator.Benchmarks.GeneratorBenchmarks).Assembly).Run(args);
