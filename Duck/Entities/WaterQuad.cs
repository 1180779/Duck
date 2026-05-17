namespace Duck.Entities;

public sealed class WaterQuad : Quad
{
    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}