using JungleMaze.App.Ports;

namespace JungleMaze.App.Visibility
{
    /// <summary>Default <see cref="IVisibilityPolicy"/> until FOW-01 exists: everything is visible.</summary>
    public sealed class AlwaysVisiblePolicy : IVisibilityPolicy
    {
        public bool IsVisible(float x, float y, byte netId) => true;
    }
}
