#nullable disable
using System;
namespace JungleMaze.World.Airdrops
{
    [Serializable]
    public sealed class AirdropDropSettings
    {
        public int Id;
        public float FlyAt, LandAt;
    }

    [Serializable]
    public sealed class AirdropSettings
    {
        public string Version = "1.0.0";
        public bool Enabled = true;
        public float EggSize = 2f;
        public int EggHp = 150;
        public float EdgeMargin = 3f;
        public float RetrySeconds = 1f;
        public int MaxRetries = 5;
        public int CandidateLimit = 64;
        public int ReservedEggs = 3;
        public int ReservedLootStacks = 24;
        public AirdropDropSettings[] Drops = new AirdropDropSettings[]
        {
            new AirdropDropSettings { Id = 1, FlyAt = 110f, LandAt = 120f },
            new AirdropDropSettings { Id = 2, FlyAt = 230f, LandAt = 240f },
            new AirdropDropSettings { Id = 3, FlyAt = 320f, LandAt = 330f },
        };

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Version) || Version.Length > 48)
                throw new ArgumentException("Airdrop version must be nonempty and at most 48 characters.");
            if (!WorldMath.Finite(EggSize) || EggSize <= 0f || EggSize > 64f)
                throw new ArgumentException("Airdrop egg size must be finite, positive and at most 64.");
            if (EggHp <= 0 || EggHp > ushort.MaxValue)
                throw new ArgumentException("Airdrop egg HP must be between 1 and 65535.");
            if (!WorldMath.Finite(EdgeMargin) || EdgeMargin < 0f)
                throw new ArgumentException("Airdrop edge margin must be finite and nonnegative.");
            if (!WorldMath.Finite(RetrySeconds) || RetrySeconds <= 0f)
                throw new ArgumentException("Airdrop retry seconds must be finite and positive.");
            if (MaxRetries < 0 || MaxRetries > 5)
                throw new ArgumentException("Airdrop max retries must be between 0 and 5.");
            if ((double)RetrySeconds * MaxRetries > 5d)
                throw new ArgumentException("Airdrop retry window must not exceed 5 seconds.");
            if (CandidateLimit < 1 || CandidateLimit > 256)
                throw new ArgumentException("Airdrop candidate limit must be between 1 and 256.");
            if (Drops == null || Drops.Length < 1 || Drops.Length > 16)
                throw new ArgumentException("Airdrop schedule must contain between 1 and 16 drops.");
            for (var i = 0; i < Drops.Length; i++)
            {
                var drop = Drops[i];
                if (drop == null)
                    throw new ArgumentException("Airdrop drops must not be null.");
                if (drop.Id <= 0 || drop.Id > ushort.MaxValue)
                    throw new ArgumentException("Airdrop drop id must be between 1 and 65535.");
                for (var j = 0; j < i; j++)
                    if (Drops[j].Id == drop.Id)
                        throw new ArgumentException("Airdrop drop ids must be distinct.");
                if (!WorldMath.Finite(drop.FlyAt) || drop.FlyAt < 0f)
                    throw new ArgumentException("Airdrop fly time must be finite and nonnegative.");
                if (!WorldMath.Finite(drop.LandAt) || drop.LandAt <= drop.FlyAt)
                    throw new ArgumentException("Airdrop land time must be finite and after fly time.");
                if (i > 0)
                {
                    var previous = Drops[i - 1];
                    if (drop.FlyAt <= previous.FlyAt)
                        throw new ArgumentException("Airdrop fly times must strictly increase.");
                    if (drop.FlyAt < previous.LandAt)
                        throw new ArgumentException("Airdrop flights must not overlap.");
                }
            }
            if (ReservedEggs < Drops.Length || ReservedEggs > 16)
                throw new ArgumentException("Airdrop reserved eggs must cover every drop and be at most 16.");
            if (ReservedLootStacks < 8L * Drops.Length || ReservedLootStacks > 128)
                throw new ArgumentException("Airdrop reserved loot stacks must cover eight stacks per drop and be at most 128.");
        }
    }
}
