
public interface ITerraform
{
    public IntRect Area { get; }

    // Guaranteed to be within the area
    public void Apply(Terrain terrain)
    {
        
    }
}

public class RandomStoneTerraform : ITerraform
{
    public IntRect Area { get; }

    public RandomStoneTerraform(IntRect area)
    {
        Area = area;
    }

    public void Apply(Terrain terrain)
    {
        for (int y = Area.YMin; y < Area.YMax; y++)
        for (int x = Area.XMin; x < Area.XMax; x++)
            {
                terrain.Tiles[x, y].StoneHeight += (x * 97 + y * 89) % 100;
            }
    }
}

public class RandomSedimentTerraform : ITerraform
{
    public IntRect Area { get; }

    public RandomSedimentTerraform(IntRect area)
    {
        Area = area;
    }

    public void Apply(Terrain terrain)
    {
        for (int y = Area.YMin; y < Area.YMax; y++)
        for (int x = Area.XMin; x < Area.XMax; x++)
            {
                terrain.Tiles[x, y].SedimentHeight += (x * 97 + y * 89) % 100;
            }
    }
}

public class BlurTerraform : ITerraform
{
    public IntRect Area { get; }

    public BlurTerraform(IntRect area)
    {
        Area = area;
    }

    public void Apply(Terrain terrain)
    {
        int xStart = Area.XMin;
        int xEnd = Area.XMax;
        int yStart = Area.YMin;
        int yEnd = Area.YMax;

        if (xStart >= xEnd || yStart >= yEnd)
        {
            return;
        }

        int width = Area.XMax - Area.XMin;
        int height = Area.YMax - Area.YMin;
        int[,] blurredStone = new int[width, height];
        int[,] blurredSediment = new int[width, height];

        for (int y = yStart; y < yEnd; y++)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                TileData current = terrain.Tiles[x, y];
                int stoneSum = current.StoneHeight;
                int sedimentSum = current.SedimentHeight;

                for (int i = 0; i < 8; i++)
                {
                    uint seed = (uint)(x * 374761393 + y * 668265263 + i * 1442695041);
                    seed = seed * 1664525u + 1013904223u;
                    int sampleX = Area.XMin + (int)(seed % (uint)width);
                    seed = seed * 1664525u + 1013904223u;
                    int sampleY = Area.YMin + (int)(seed % (uint)height);

                    TileData sample = terrain.Tiles[sampleX, sampleY];
                    stoneSum += sample.StoneHeight;
                    sedimentSum += sample.SedimentHeight;
                }

                int localX = x - Area.XMin;
                int localY = y - Area.YMin;
                blurredStone[localX, localY] = stoneSum / 9;
                blurredSediment[localX, localY] = sedimentSum / 9;
            }
        }

        for (int y = yStart; y < yEnd; y++)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                int localX = x - Area.XMin;
                int localY = y - Area.YMin;
                TileData tile = terrain.Tiles[x, y];
                tile.StoneHeight = blurredStone[localX, localY];
                tile.SedimentHeight = blurredSediment[localX, localY];
                terrain.Tiles[x, y] = tile;
            }
        }
    }
}

public class TopErosionTerraform : ITerraform
{
    public IntRect Area { get; }

    public TopErosionTerraform(IntRect area)
    {
        Area = area;
    }

    public void Apply(Terrain terrain)
    {
        for (int y = Area.YMin; y < Area.YMax; y++)
            for (int x = Area.XMin; x < Area.XMax; x++)
                {
                    TileData tile = terrain.Tiles[x, y];
                    float probability = Math.Clamp((tile.StoneHeight - 100f) / 50f, 0f, 1f);
                    int roll = (x * 123 + y * 581) % 100;

                    if (roll < probability * 100f)
                    {
                        int converted = Math.Min(20, tile.StoneHeight);
                        tile.StoneHeight -= converted;
                        tile.SedimentHeight += converted;
                        terrain.Tiles[x, y] = tile;
                    }
                }
    }
}