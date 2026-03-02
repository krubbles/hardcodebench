ITerraformRunner runnerA = new TestSolveTerraformRunner();
ITerraformRunner runnerB = new ReferenceSolveTerraformRunner();

TerraformRunnerProfiler profiler = new();
TerraformRunnerProfileResult result = profiler.Profile(runnerA, runnerB, thresholdSeconds: 2.0);

Console.WriteLine($"testSolve: n={result.RunnerA.Samples}, mean={result.RunnerA.MeanMilliseconds:F3}ms, stddev={result.RunnerA.StandardDeviationMilliseconds:F3}ms");
Console.WriteLine($"referenceSolve: n={result.RunnerB.Samples}, mean={result.RunnerB.MeanMilliseconds:F3}ms, stddev={result.RunnerB.StandardDeviationMilliseconds:F3}ms");
Console.WriteLine($"Winner: {result.Winner}, confidence={result.Confidence:P2}");
