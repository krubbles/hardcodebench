using System.Runtime.CompilerServices;

public sealed class ReferenceSolveTerraformRunner : ITerraformRunner
{
    private int[] _blurStone = new int[70 * 70];
    private int[] _blurSediment = new int[70 * 70];

    public void Run(Terrain terrain, ITerraform[] terraforms)
    {
        TileData[,] tiles = terrain.Tiles;

        for (int i = 0; i < terraforms.Length; i++)
        {
            ITerraform terraform = terraforms[i];

            if (terraform is RandomStoneTerraform randomStone)
            {
                ApplyRandomStone(tiles, randomStone.Area);
                continue;
            }

            if (terraform is RandomSedimentTerraform randomSediment)
            {
                ApplyRandomSediment(tiles, randomSediment.Area);
                continue;
            }

            if (terraform is BlurTerraform blur)
            {
                ApplyBlur(tiles, blur.Area);
                continue;
            }

            if (terraform is TopErosionTerraform erosion)
            {
                ApplyTopErosion(tiles, erosion.Area);
                continue;
            }

            terraform.Apply(terrain);
        }
    }

    private static void ApplyRandomStone(TileData[,] tiles, IntRect area)
    {
        for (int y = area.YMin; y < area.YMax; y++)
        {
            int value = y * 89 + area.XMin * 97;
            for (int x = area.XMin; x < area.XMax; x++)
            {
                TileData tile = tiles[x, y];
                tile.StoneHeight += value % 100;
                tiles[x, y] = tile;
                value += 97;
            }
        }
    }

    private static void ApplyRandomSediment(TileData[,] tiles, IntRect area)
    {
        for (int y = area.YMin; y < area.YMax; y++)
        {
            int value = y * 89 + area.XMin * 97;
            for (int x = area.XMin; x < area.XMax; x++)
            {
                TileData tile = tiles[x, y];
                tile.SedimentHeight += value % 100;
                tiles[x, y] = tile;
                value += 97;
            }
        }
    }

    private void ApplyBlur(TileData[,] tiles, IntRect area)
    {
        int xStart = area.XMin;
        int xEnd = area.XMax;
        int yStart = area.YMin;
        int yEnd = area.YMax;

        if (xStart >= xEnd || yStart >= yEnd)
        {
            return;
        }

        int width = area.XMax - area.XMin;
        int height = area.YMax - area.YMin;
        uint widthU = (uint)width;
        uint heightU = (uint)height;
        int xMin = area.XMin;
        int yMin = area.YMin;
        int required = width * height;
        EnsureBlurCapacity(required);

        for (int y = yStart; y < yEnd; y++)
        {
            int localY = y - area.YMin;
            int rowBase = localY * width;

            for (int x = xStart; x < xEnd; x++)
            {
                TileData current = tiles[x, y];
                int stoneSum = current.StoneHeight;
                int sedimentSum = current.SedimentHeight;

                for (int i = 0; i < 8; i++)
                {
                    uint seed = unchecked((uint)(x * 374761393 + y * 668265263 + i * 1442695041));
                    seed = seed * 1664525u + 1013904223u;
                    int sampleX = xMin + (int)(seed % widthU);
                    seed = seed * 1664525u + 1013904223u;
                    int sampleY = yMin + (int)(seed % heightU);

                    TileData sample = tiles[sampleX, sampleY];
                    stoneSum += sample.StoneHeight;
                    sedimentSum += sample.SedimentHeight;
                }

                int localX = x - area.XMin;
                int index = rowBase + localX;
                _blurStone[index] = stoneSum / 9;
                _blurSediment[index] = sedimentSum / 9;
            }
        }

        for (int y = yStart; y < yEnd; y++)
        {
            int localY = y - area.YMin;
            int rowBase = localY * width;

            for (int x = xStart; x < xEnd; x++)
            {
                int localX = x - area.XMin;
                int index = rowBase + localX;
                TileData tile = tiles[x, y];
                tile.StoneHeight = _blurStone[index];
                tile.SedimentHeight = _blurSediment[index];
                tiles[x, y] = tile;
            }
        }
    }

    private static void ApplyTopErosion(TileData[,] tiles, IntRect area)
    {
        for (int y = area.YMin; y < area.YMax; y++)
        {
            for (int x = area.XMin; x < area.XMax; x++)
            {
                TileData tile = tiles[x, y];
                float probability = Math.Clamp((tile.StoneHeight - 100f) / 50f, 0f, 1f);
                int roll = (x * 123 + y * 581) % 100;
                if (roll < probability * 100f)
                {
                    int converted = tile.StoneHeight;
                    if (converted > 20)
                    {
                        converted = 20;
                    }

                    tile.StoneHeight -= converted;
                    tile.SedimentHeight += converted;
                    tiles[x, y] = tile;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureBlurCapacity(int required)
    {
        if (_blurStone.Length < required)
        {
            _blurStone = new int[required];
            _blurSediment = new int[required];
        }
    }
}