using JungleMaze.Domain.Vfx;

namespace JungleMaze.App.Vfx
{
    /// <summary>
    /// Draws VFX instances (Presentation: <c>VfxStage</c> over the CLI-03 pool). Handles are opaque and carry a
    /// generation, so releasing an old handle after its slot was reused does nothing.
    /// </summary>
    public interface IVfxOutput
    {
        /// <summary>Returns 0 when nothing could be spawned (no prefab for the cue, no free slot).</summary>
        int Spawn(VfxCue cue, in VfxSpawn spawn);
        /// <summary>Returns the instance to the pool now. Unknown or stale handles are ignored.</summary>
        void Release(int handle);
        void ReleaseAll();
        /// <summary>Tints the body of <paramref name="netId"/> for <paramref name="seconds"/> (extends, never stacks).</summary>
        void Flash(byte netId, float seconds);
        void CancelFlash(byte netId);
        int ActiveCount { get; }
    }
}
