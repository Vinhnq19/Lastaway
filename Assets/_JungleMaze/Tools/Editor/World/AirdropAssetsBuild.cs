using System.IO;
using System.Linq;
using JungleMaze.Presentation.World.Phase2;
using UnityEditor;
using UnityEngine;
namespace JungleMaze.EditorTools
{
    public static class AirdropAssetsBuild
    {
        [MenuItem("Jungle Maze/Dragon/Build Airdrop Assets")]
        public static void Build()
        {
            const string folder="Assets/_JungleMaze/Art/World/Resources";
            var set=AssetDatabase.LoadAssetAtPath<AirdropVisualSet>(folder+"/AirdropVisualSet.asset");
            if(set==null){set=ScriptableObject.CreateInstance<AirdropVisualSet>();AssetDatabase.CreateAsset(set,folder+"/AirdropVisualSet.asset");}
            var world=Resources.Load<WorldVisualSet>("WorldVisualSet");
            set.Egg=world!=null?world.Find(13):null;
            var path=AssetDatabase.FindAssets("DRAGONJS.move t:AnimationClip").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>p.EndsWith("/DRAGONJS.move.anim"));
            if(path==null||set.Egg==null)throw new System.InvalidOperationException("Existing dragon animation/egg art missing.");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b=>b.propertyName=="m_Sprite");
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            set.Dragon=new WorldVisualSet.Frames{Sprites=keys.Select(k=>(Sprite)k.value).ToArray(),Times=keys.Select(k=>k.time).ToArray(),Duration=clip.length,Loop=true};
            var disc=folder+"/AirdropDisc.png";
            if(!File.Exists(disc))
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                var pixels=new Color[4096];for(var y=0;y<64;y++)for(var x=0;x<64;x++)pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))));
                texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(disc,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(disc);
                var importer=(TextureImporter)AssetImporter.GetAtPath(disc);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            }
            set.Disc=AssetDatabase.LoadAssetAtPath<Sprite>(disc);EditorUtility.SetDirty(set);
            var root=new GameObject("AirdropVisual");var view=root.AddComponent<AirdropVisual>();view.Visuals=set;
            PrefabUtility.SaveAsPrefabAsset(root,folder+"/AirdropVisual.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            Debug.Log("[DRG-01] Built dragon/egg visual set and prefab from existing artwork.");
        }
    }
}
