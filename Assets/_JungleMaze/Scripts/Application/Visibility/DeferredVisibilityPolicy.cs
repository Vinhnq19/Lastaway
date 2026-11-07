using System;
using JungleMaze.App.Ports;

namespace JungleMaze.App.Visibility
{
    /// <summary>
    /// Visibility used by VFX when no <see cref="IVisibilityPolicy"/> was registered at composition time. It is not
    /// registered itself, so FOW-01 can still register the real policy later without a duplicate-type error. Each query
    /// asks the lookup until it returns a policy, then forwards to that policy for good; until then everything is visible.
    /// </summary>
    public sealed class DeferredVisibilityPolicy : IVisibilityPolicy
    {
        private readonly Func<IVisibilityPolicy> _lookup;
        private IVisibilityPolicy _resolved;

        /// <param name="lookup">Returns the registered policy, or null while there is none. Must not allocate.</param>
        public DeferredVisibilityPolicy(Func<IVisibilityPolicy> lookup) => _lookup = lookup;

        /// <summary>True once a real policy was found and is being forwarded to.</summary>
        public bool IsResolved => _resolved != null;

        public bool IsVisible(float x, float y, byte netId)
        {
            if (_resolved == null && _lookup != null)
            {
                var found = _lookup();
                if (found != null && !ReferenceEquals(found, this)) _resolved = found;
            }
            return _resolved == null || _resolved.IsVisible(x, y, netId);
        }
    }
}
