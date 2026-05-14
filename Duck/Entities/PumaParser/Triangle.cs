namespace Duck.Entities.PumaParser;

public sealed record Triangle
{
    public int VertexIdxIdx1;
    public int VertexIdxIdx2;
    public int VertexIdxIdx3;

    public static Triangle Parse(string s)
    {
        var nums = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nums.Length != 3)
        {
            throw new FormatException($"Triangle: expected 3 values, got {nums.Length}.");
        }

        return new Triangle
        {
            VertexIdxIdx1 = int.Parse(nums[0]),
            VertexIdxIdx2 = int.Parse(nums[1]),
            VertexIdxIdx3 = int.Parse(nums[2]),
        };
    }
}