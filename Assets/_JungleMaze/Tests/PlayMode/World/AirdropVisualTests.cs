using System.Collections;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.Presentation.World.Phase2;
using JungleMaze.Presentation.Audio;
using JungleMaze.Domain.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace JungleMaze.Tests.World
{
    public sealed class AirdropVisualTests
    {
        [UnityTest] public IEnumerator CancelRemovesExistingFlightView()
        {
            var root=new GameObject("Airdrop cancellation test");var scene=root.AddComponent<AirdropSceneView>();scene.Construct(null,null,null,null);
            var draw=typeof(AirdropSceneView).GetMethod("Draw",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var p=new AirdropStatePacket{DropId=1,Phase=1,Revision=1,Size=2,MaxHp=150,FlyTick=1,FallTick=2,LandTick=3};
            draw.Invoke(scene,new object[]{p,1f,System.Array.Empty<WorldObjectWire>(),50});Assert.AreEqual(1,root.transform.childCount);
            p.Phase=5;p.Revision++;draw.Invoke(scene,new object[]{p,2f,System.Array.Empty<WorldObjectWire>(),50});yield return null;
            Assert.AreEqual(0,root.transform.childCount);Object.Destroy(root);
        }
        [UnityTest] public IEnumerator RealPrefabShowsFlightFallHealthBreakAndBaseline()
        {
            var prefab=Resources.Load<AirdropVisual>("AirdropVisual");Assert.IsNotNull(prefab);
            var view=Object.Instantiate(prefab);yield return null;
            try
            {
                var p=new AirdropStatePacket{DropId=1,Phase=1,Revision=1,X=25,Y=25,EntryX=-8,EntryY=25,ExitX=58,ExitY=25,FlyTick=3300,FallTick=3450,LandTick=3600,Size=2,MaxHp=150};
                SpriteRenderer Part(string name)=>view.transform.Find(name).GetComponent<SpriteRenderer>();
                view.Show(p,3350,false,150);Assert.IsTrue(Part("Dragon").enabled);Assert.IsNotNull(Part("Dragon").sprite);Assert.IsFalse(Part("Falling egg").enabled);Assert.IsTrue(Part("Ground shadow").enabled);
                p.Phase=2;p.Revision++;view.Show(p,3500,false,150);Assert.IsTrue(Part("Falling egg").enabled);Assert.Greater(Part("Falling egg").transform.position.y,p.Y);
                p.Phase=3;p.Revision++;view.Show(p,3600,false,149);Assert.AreEqual(p.Y-p.Size*.5f,Part("Falling egg").bounds.min.y,.01f);view.Show(p,3600,true,149);Assert.IsFalse(Part("Falling egg").enabled);Assert.IsFalse(Part("Dragon").enabled);Assert.IsTrue(Part("Egg HP").enabled);
                p.Phase=4;p.Revision++;view.Show(p,3610,true,0);Assert.IsTrue(Part("Shell 0").enabled);Assert.IsFalse(Part("Egg HP").enabled);
                yield return new WaitForSecondsRealtime(.7f);view.Show(p,3640,true,0);Assert.IsFalse(Part("Shell 0").enabled);
                var baseline=Object.Instantiate(prefab);yield return null;
                p.Baseline=true;baseline.Show(p,3700,true,0);Assert.IsFalse(baseline.transform.Find("Shell 0").GetComponent<SpriteRenderer>().enabled);Object.Destroy(baseline.gameObject);
                foreach(var id in new[]{SfxId.EggLand,SfxId.EggBreak})
                {
                    var clip=Resources.Load<AudioRuntimeSet>("AudioRuntimeSet").Catalog.GetClip(id,0);Assert.IsNotNull(clip);clip.LoadAudioData();yield return null;
                    var data=new float[clip.samples*clip.channels];Assert.IsTrue(clip.GetData(data,0));Assert.IsTrue(System.Array.Exists(data,v=>Mathf.Abs(v)>.01f));
                }
            }
            finally{Object.Destroy(view.gameObject);}
        }
    }
}
