using System.Collections.Generic;
using JungleMaze.App.Ports;
using JungleMaze.App.Vfx;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// <see cref="IVfxTargetLocator"/> over the live <see cref="VfxBodyAnchor"/>s (plan 4.12.1, R3): online the own body is
    /// the own <c>PlayerView</c> (the Hero is a hidden camera proxy), offline it is the Hero anchor; other players are
    /// their PlayerView. Without an anchor the pose source is used for the position only (no flash). A hidden or dead
    /// body counts as missing, so nothing is drawn on it.
    /// </summary>
    public sealed class VfxAnchorRegistry : IVfxTargetLocator
    {
        private readonly List<VfxBodyAnchor> _anchors = new List<VfxBodyAnchor>(16);
        private readonly IVfxMatchFeed _feed;
        private readonly IPlayerPoseSource _poses;

        public VfxAnchorRegistry(IVfxMatchFeed feed, IPlayerPoseSource poses = null)
        {
            _feed = feed;
            _poses = poses;
        }

        public int Count => _anchors.Count;

        public void Add(VfxBodyAnchor anchor)
        {
            if (anchor != null && !_anchors.Contains(anchor)) _anchors.Add(anchor);
        }

        public void Remove(VfxBodyAnchor anchor) => _anchors.Remove(anchor);

        /// <summary>The body drawn for <paramref name="netId"/> under the own-body rule above.</summary>
        public bool TryGetAnchor(byte netId, out VfxBodyAnchor anchor)
        {
            anchor = null;
            if (netId == VfxSpawn.NoAnchor) return false;
            var own = _feed != null && netId == _feed.OwnNetId;
            var online = _feed != null && _feed.Online;
            VfxBodyAnchor fallback = null;
            for (var i = _anchors.Count - 1; i >= 0; i--)
            {
                var a = _anchors[i];
                if (a == null) { _anchors.RemoveAt(i); continue; }
                if (!a.IsShowing) continue;
                if (a.LocalHero)
                {
                    if (own && !online) { anchor = a; return true; }
                    continue;
                }
                if (a.NetId != netId) continue;
                if (!own || online) { anchor = a; return true; }
                fallback = a; // offline own body without a Hero anchor (preview, tests)
            }
            anchor = fallback;
            return anchor != null;
        }

        public bool TryGetPosition(byte netId, out float x, out float y)
        {
            if (TryGetAnchor(netId, out var anchor))
            {
                var p = anchor.transform.position;
                x = p.x;
                y = p.y;
                return true;
            }
            if (_poses != null && netId != VfxSpawn.NoAnchor && _poses.TryGetPose(netId, out var pose) && !pose.IsDead)
            {
                x = pose.X;
                y = pose.Y;
                return true;
            }
            x = y = 0f;
            return false;
        }
    }
}
