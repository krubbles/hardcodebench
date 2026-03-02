using System.Diagnostics;

public enum RunnerWinner
{
    RunnerA,
    RunnerB,
    Tie
}

public readonly record struct RunnerStats(int Samples, double MeanMilliseconds, double StandardDeviationMilliseconds);

public readonly record struct TerraformRunnerProfileResult(
    RunnerStats RunnerA,
    RunnerStats RunnerB,
    RunnerWinner Winner,
    double Confidence);

public sealed class TerraformRunnerProfiler
{
    private readonly Random _random = new();

    public TerraformRunnerProfileResult Profile(
        ITerraformRunner runnerA,
        ITerraformRunner runnerB,
        double thresholdSeconds)
    {
        if (thresholdSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdSeconds));
        }

        List<double> samplesA = [];
        List<double> samplesB = [];
        Stopwatch wallClock = Stopwatch.StartNew();

        while (samplesA.Count == 0 || wallClock.Elapsed.TotalSeconds < thresholdSeconds)
        {
            ITerraform[] terraforms = GenerateTerraforms();

            Terrain terrainA = new();
            Terrain terrainB = new();

            samplesA.Add(MeasureMilliseconds(() => runnerA.Run(terrainA, terraforms)));
            samplesB.Add(MeasureMilliseconds(() => runnerB.Run(terrainB, terraforms)));

            ValidateSameTerrain(terrainA, terrainB);
        }

        RunnerStats statsA = ComputeStats(samplesA);
        RunnerStats statsB = ComputeStats(samplesB);

        RunnerWinner winner;
        double diff = statsB.MeanMilliseconds - statsA.MeanMilliseconds;
        if (Math.Abs(diff) < double.Epsilon)
        {
            winner = RunnerWinner.Tie;
        }
        else
        {
            winner = diff > 0 ? RunnerWinner.RunnerA : RunnerWinner.RunnerB;
        }

        double confidence = ComputeConfidence(statsA, statsB);

        return new TerraformRunnerProfileResult(statsA, statsB, winner, confidence);
    }

    private static double MeasureMilliseconds(Action action)
    {
        long start = Stopwatch.GetTimestamp();
        action();
        long end = Stopwatch.GetTimestamp();
        return (end - start) * 1000.0 / Stopwatch.Frequency;
    }

    private static RunnerStats ComputeStats(List<double> samples)
    {
        double mean = samples.Average();

        if (samples.Count == 1)
        {
            return new RunnerStats(1, mean, 0);
        }

        double sumSquares = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            double delta = samples[i] - mean;
            sumSquares += delta * delta;
        }

        double variance = sumSquares / (samples.Count - 1);
        return new RunnerStats(samples.Count, mean, Math.Sqrt(variance));
    }

    private static double ComputeConfidence(RunnerStats statsA, RunnerStats statsB)
    {
        if (Math.Abs(statsA.MeanMilliseconds - statsB.MeanMilliseconds) < double.Epsilon)
        {
            return 0.0;
        }

        double varianceA = statsA.StandardDeviationMilliseconds * statsA.StandardDeviationMilliseconds;
        double varianceB = statsB.StandardDeviationMilliseconds * statsB.StandardDeviationMilliseconds;
        double standardError = Math.Sqrt((varianceA / statsA.Samples) + (varianceB / statsB.Samples));

        if (standardError <= double.Epsilon)
        {
            return 1.0;
        }

        double zScore = Math.Abs(statsA.MeanMilliseconds - statsB.MeanMilliseconds) / standardError;
        double twoSidedP = 2.0 * (1.0 - NormalCdf(zScore));
        return Math.Clamp(1.0 - twoSidedP, 0.0, 1.0);
    }

    internal static double NormalCdf(double x)
    {
        double absX = Math.Abs(x);
        double t = 1.0 / (1.0 + 0.2316419 * absX);
        double d = 0.3989423 * Math.Exp(-absX * absX / 2.0);
        double probability = 1.0 - d * t *
            (0.3193815 + t * (-0.3565638 + t * (1.781478 + t * (-1.821256 + t * 1.330274))));

        return x >= 0 ? probability : 1.0 - probability;
    }

    private static void ValidateSameTerrain(Terrain terrainA, Terrain terrainB)
    {
        for (int y = 0; y < Terrain.GridSize; y++)
        {
            for (int x = 0; x < Terrain.GridSize; x++)
            {
                TileData a = terrainA.Tiles[x, y];
                TileData b = terrainB.Tiles[x, y];

                if (a.StoneHeight != b.StoneHeight || a.SedimentHeight != b.SedimentHeight)
                {
                    throw new InvalidOperationException(
                        $"Runner outputs differ at ({x}, {y}): " +
                        $"A=(stone:{a.StoneHeight}, sediment:{a.SedimentHeight}) " +
                        $"B=(stone:{b.StoneHeight}, sediment:{b.SedimentHeight})");
                }
            }
        }
    }

    private ITerraform[] GenerateTerraforms()
    {
        int[] sizeSet = [2, 3, 4, 6, 10, 31, 43, 70];
        ITerraform[] terraforms = new ITerraform[1000];

        for (int i = 0; i < terraforms.Length; i++)
        {
            int width = sizeSet[_random.Next(sizeSet.Length)];
            int height = sizeSet[_random.Next(sizeSet.Length)];

            int xMin = _random.Next(0, Terrain.GridSize - width + 1);
            int yMin = _random.Next(0, Terrain.GridSize - height + 1);
            IntRect area = new(xMin, yMin, xMin + width, yMin + height);

            int terraformIndex = _random.Next(4);
            terraforms[i] = terraformIndex switch
            {
                0 => new RandomStoneTerraform(area),
                1 => new RandomSedimentTerraform(area),
                2 => new BlurTerraform(area),
                _ => new TopErosionTerraform(area)
            };
        }

        return terraforms;
    }
}