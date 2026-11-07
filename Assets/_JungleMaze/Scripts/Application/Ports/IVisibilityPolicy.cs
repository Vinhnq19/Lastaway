namespace JungleMaze.App.Ports
{
    /// <summary>
    /// Fog-of-war seam (VFX-01 spec 2.4): may the local player see a sign at world position (x, y), optionally on the
    /// body of <paramref name="netId"/>? FOW-01 owns the policy and registers it, before or after VFX is composed
    /// (see <c>DeferredVisibilityPolicy</c>); until then everything is visible. This is presentation only, not an anti-cheat measure (SRV-03 filters state).
    /// </summary>
    public interface IVisibilityPolicy
    {
        /// <param name="netId"><see cref="PositionOnly"/> when asking about a position rather than a body.</param>
        bool IsVisible(float x, float y, byte netId);
    }

    /// <summary>Constants for <see cref="IVisibilityPolicy"/>.</summary>
    public static class VisibilityQuery
    {
        public const byte PositionOnly = 255;
    }
}
