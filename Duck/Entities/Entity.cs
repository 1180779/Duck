using Silk.NET.Input;

namespace Duck.Entities;
public abstract class Entity
{
    public virtual void HandleInput(IKeyboard keyboard, IMouse mouse, float dt) { }
    public virtual void Update(float dt) { }
    public virtual void Render(Camera camera) { }
}