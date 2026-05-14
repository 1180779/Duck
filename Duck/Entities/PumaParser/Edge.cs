namespace Duck.Entities.PumaParser;

public sealed record Edge
{
    public int VertexPosIdx1;
    public int VertexPosIdx2;
    public int TriangleIdx1;
    public int TriangleIdx2;

    public static Edge Parse(string s)
    {
        var nums = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nums.Length != 4)
        {
            throw new FormatException($"Edge: expected 4 values, got {nums.Length}.");
        }

        return new Edge
        {
            VertexPosIdx1 = int.Parse(nums[0]),
            VertexPosIdx2 = int.Parse(nums[1]),
            TriangleIdx1 = int.Parse(nums[2]),
            TriangleIdx2 = int.Parse(nums[3]),
        };
    }
}