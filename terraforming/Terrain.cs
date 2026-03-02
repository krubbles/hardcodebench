using System.Runtime.CompilerServices;

public class Terrain
{
    public const int GridSize = 256;
    
    public readonly TileData[,] Tiles = new TileData[GridSize, GridSize];

    public Terrain()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                Tiles[x, y] = new TileData(0, 0);
            }
        }
    }
}

public record struct TileData(int StoneHeight, int SedimentHeight);