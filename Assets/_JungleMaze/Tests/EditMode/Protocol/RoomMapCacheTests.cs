using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JungleMaze.App.Config;
using JungleMaze.Infrastructure.Api;
using JungleMaze.Infrastructure.Config;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JungleMaze.Tests.Protocol
{
    public sealed class RoomMapCacheTests
    {
        private string _root;
        private string Source => Path.GetFullPath(Path.Combine(Application.dataPath,"../Backend/Shared/GameData"));
        private const string Api = "http://127.0.0.1:1"; // No listener: invalid cache must fail rather than silently load stale data.
        [SetUp] public void Setup() => _root = Path.Combine(Path.GetTempPath(),"lastaway-map-cache-"+Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            var absolute = Path.GetFullPath(_root);
            Assert.AreEqual(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar),Path.GetDirectoryName(absolute));
            if (Directory.Exists(absolute)) Directory.Delete(absolute,true);
        }
        private RoomMapFiles Files(string api = Api) => new RoomMapFiles(() => new ClientConfig(ConnectionMode.Local,api,"127.0.0.1",7777,JungleBackend.Protocol.JoinAuthMode.RoomCode,4,5000),()=>"test-only-not-a-credential",new ClientRuntimeData(Application.streamingAssetsPath),_root,Application.streamingAssetsPath);
        private string Cache(string api,string hash,string extension)
        {
            var directory = Path.Combine(_root,Sha(Encoding.UTF8.GetBytes(new Uri(api).AbsoluteUri.TrimEnd('/'))));
            Directory.CreateDirectory(directory); return Path.Combine(directory,hash+extension);
        }
        [Test]
        public void CurrentMapLoadsButWrongRulesAndWrongMapHashStayUnready()
        {
            var catalog = JObject.Parse(File.ReadAllText(Path.Combine(Source,"Maps/catalog.json")));
            foreach (var entry in catalog["maps"])
            {
                var hash = (string)entry["mapHash"];
                var json = File.ReadAllText(Path.Combine(Source,"Maps",(string)entry["id"],hash,"map.json"));
                var runtime = new ClientRuntimeData(Application.streamingAssetsPath);
                StringAssert.Contains("Luật chơi client khác server", runtime.ActivateSelectedMap(json,hash,new string('f',64)));
                Assert.IsNull(runtime.SelectedLayout);
                StringAssert.Contains("Map tải về khác", runtime.ActivateSelectedMap(json,new string('f',64),RulesHash()));
                Assert.IsNull(runtime.SelectedLayout);
                Assert.IsNull(runtime.ActivateSelectedMap(json,hash,RulesHash()));
                Assert.IsNotNull(runtime.SelectedLayout);
            }
        }

        [UnityTest]
        public IEnumerator CorruptPreviewCannotPassHashVerification_AndRetryUsesRepairedCache()
        {
            var catalog=JObject.Parse(File.ReadAllText(Path.Combine(Source,"Maps/catalog.json")));var entry=catalog["maps"][0];
            var hash=(string)entry["previewHash"];
            var image=File.ReadAllBytes(Path.Combine(Source,"Maps",(string)entry["id"],(string)entry["mapHash"],hash+".png"));
            var path=Cache(Api,hash,".png");var corrupt=(byte[])image.Clone();corrupt[corrupt.Length-1]^=1;File.WriteAllBytes(path,corrupt);
            var failed=Files().Preview(hash,CancellationToken.None);yield return Complete(failed);
            Assert.IsTrue(failed.IsFaulted,"A valid PNG header cannot bypass content hash verification.");_ = failed.Exception;
            Assert.IsFalse(File.Exists(path));File.WriteAllBytes(path,image);
            var retry=Files().Preview(hash,CancellationToken.None);yield return Complete(retry);
            Assert.IsFalse(retry.IsFaulted);CollectionAssert.AreEqual(image,retry.Result);
        }
        [UnityTest]
        public IEnumerator InvalidAndOversizedMapCachesRemainUnready_ThenValidCacheCanBeActivated()
        {
            var catalog=JObject.Parse(File.ReadAllText(Path.Combine(Source,"Maps/catalog.json")));var entry=catalog["maps"][1];
            var hash=(string)entry["mapHash"];var id=(string)entry["id"];
            var json=File.ReadAllText(Path.Combine(Source,"Maps",id,hash,"map.json")).Replace("\r\n","\n");
            var rules=RulesHash();var path=Cache(Api,hash,".json");var files=Files();
            foreach(var bytes in new[]{Encoding.UTF8.GetBytes("{}"),new byte[2*1024*1024+1]})
            {
                File.WriteAllBytes(path,bytes);var bad=files.Map(id,hash,rules,CancellationToken.None);yield return Complete(bad);
                Assert.IsTrue(bad.IsFaulted);_ = bad.Exception;Assert.IsFalse(File.Exists(path));
            }
            File.WriteAllText(path,json,new UTF8Encoding(false));var valid=files.Map(id,hash,rules,CancellationToken.None);yield return Complete(valid);
            Assert.IsFalse(valid.IsFaulted);Assert.IsNull(files.Activate(valid.Result,hash,rules));
        }
        [UnityTest]
        public IEnumerator PreviewCacheIsIsolatedByBackendOrigin()
        {
            var entry=JObject.Parse(File.ReadAllText(Path.Combine(Source,"Maps/catalog.json")))["maps"][0];var hash=(string)entry["previewHash"];
            var image=File.ReadAllBytes(Path.Combine(Source,"Maps",(string)entry["id"],(string)entry["mapHash"],hash+".png"));
            var other=Cache("https://other-backend.invalid",hash,".png");File.WriteAllBytes(other,image);
            var result=Files().Preview(hash,CancellationToken.None);yield return Complete(result);
            Assert.IsTrue(result.IsFaulted,"Another backend's cache cannot satisfy this origin.");_ = result.Exception;Assert.IsTrue(File.Exists(other));
        }
        [TestCase("http://192.168.1.2:5000")]
        [TestCase("https://user:password@example.invalid")]
        public void RemotePlainHttpAndCredentialUrlsAreRejected(string url)
        { Assert.ThrowsAsync<InvalidOperationException>(async()=>await Files(url).Catalog(CancellationToken.None)); }
        private string RulesHash()
        {
            using var stream=new MemoryStream();foreach(var file in new[]{"match_settings.json","zone_schedule.json","world_settings.json","items_catalog.json","weapons.json","airdrop_schedule.json"})
                foreach(var bytes in new[]{Encoding.UTF8.GetBytes(file),Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(Source,file)).Replace("\r\n","\n"))}) stream.Write(bytes,0,bytes.Length);
            return Sha(stream.ToArray());
        }
        private static string Sha(byte[] bytes)
        { using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        private static IEnumerator Complete(Task task)
        { var deadline=Time.realtimeSinceStartup+20;while(!task.IsCompleted&&Time.realtimeSinceStartup<deadline)yield return null;Assert.IsTrue(task.IsCompleted,"Bounded map request did not complete."); }
    }
}
