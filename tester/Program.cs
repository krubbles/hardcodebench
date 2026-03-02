ITerraformRunner runnerA = new SequentialTerraformRunner();
ITerraformRunner runnerB = new SequentialTerraformRunner();

TerraformRunnerProfiler profiler = new();
TerraformRunnerProfileResult result = profiler.Profile(runnerA, runnerB, thresholdSeconds: 2.0);

Console.WriteLine($"Runner A: n={result.RunnerA.Samples}, mean={result.RunnerA.MeanMilliseconds:F3}ms, stddev={result.RunnerA.StandardDeviationMilliseconds:F3}ms");
Console.WriteLine($"Runner B: n={result.RunnerB.Samples}, mean={result.RunnerB.MeanMilliseconds:F3}ms, stddev={result.RunnerB.StandardDeviationMilliseconds:F3}ms");
Console.WriteLine($"Winner: {result.Winner}, confidence={result.Confidence:P2}");
