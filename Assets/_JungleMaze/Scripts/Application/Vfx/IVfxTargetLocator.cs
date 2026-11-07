namespace JungleMaze.App.Vfx
{
    /// <summary>Finds the drawn body of a player (Presentation: anchors on PlayerView/Hero, pose fallback).</summary>
    public interface IVfxTargetLocator
    {
        bool TryGetPosition(byte netId, out float x, out float y);
    }
}
