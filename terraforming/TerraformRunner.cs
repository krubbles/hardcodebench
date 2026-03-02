public interface ITerraformRunner
{
    void Run(Terrain terrain, ITerraform[] terraforms);
}

public sealed class SequentialTerraformRunner : ITerraformRunner
{
    public void Run(Terrain terrain, ITerraform[] terraforms)
    {
        for (int i = 0; i < terraforms.Length; i++)
        {
            terraforms[i].Apply(terrain);
        }
    }
}