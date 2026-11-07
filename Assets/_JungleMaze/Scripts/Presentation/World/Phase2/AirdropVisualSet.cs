using UnityEngine;
namespace JungleMaze.Presentation.World.Phase2
{
    [CreateAssetMenu(menuName="Jungle Maze/Airdrop visuals")]
    public sealed class AirdropVisualSet : ScriptableObject
    {
        public WorldVisualSet.Frames Dragon;
        public Sprite Egg,Disc;
    }
}
