using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using JungleMaze.App.Audio;
using JungleMaze.Presentation.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.Tests.Audio
{
    /// <summary>Streamed music under Art/Audio/Music, including the user-provided menu loop, wired in AudioRuntimeSet.</summary>
    public sealed class ArtAudioMusicTests
    {
        private const string RuntimeSetPath = "Assets/_JungleMaze/Art/Audio/Resources/AudioRuntimeSet.asset";

        [TestCase(MusicTrack.Loading, "loading.ogg", 43.65f)]
        [TestCase(MusicTrack.Match, "match.ogg", 123.48f)]
        public void Track_Compressed_Streamed_Wired(MusicTrack track, string file, float sourceSeconds)
        {
            var set = AssetDatabase.LoadAssetAtPath<AudioRuntimeSet>(RuntimeSetPath);
            var clip = set.MusicFor(track);
            Assert.IsNotNull(clip, track.ToString());
            var path = AssetDatabase.GetAssetPath(clip);
            Assert.AreEqual(ArtAudioSpec.MusicFolder + file, path);
            Assert.LessOrEqual(new FileInfo(ArtAudioSpec.ProjectPath(path)).Length, 3_000_000, path + " ≤ 3.0 MB");
            Assert.AreEqual(sourceSeconds, clip.length, 0.1f, path + " length");
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType, path);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, importer.defaultSampleSettings.compressionFormat, path);
            Assert.IsTrue(importer.loadInBackground, path);
            StringAssert.Contains("\n  normalize: 0", File.ReadAllText(ArtAudioSpec.ProjectPath(path + ".meta")).Replace("\r", ""), path);
        }

        // Review G1: Unity re-encodes the OGG, so the importer uses the quality the file was made with (no second,
        // higher-quality pass on an already lossy source) and the imported size stays under the 3.0 MB budget.
        [TestCase("waiting_room.ogg")]
        [TestCase("loading.ogg")]
        [TestCase("match.ogg")]
        public void Importer_QualityOfTheSource_ImportedSizeInBudget(string file)
        {
            var row = ArtAudioSpec.ReadManifest().Single(r => r["row_type"] == "clip" && r["file"] == "Music/" + file);
            var quality = float.Parse(row["codec"].Substring(row["codec"].LastIndexOf('q') + 1), CultureInfo.InvariantCulture);
            var importer = (AudioImporter)AssetImporter.GetAtPath(ArtAudioSpec.MusicFolder + file);
            Assert.AreEqual(quality, importer.defaultSampleSettings.quality, 1e-3f, file);
            // AudioImporter exposes the imported size only internally (compSize).
            var size = typeof(AudioImporter).GetProperty("compSize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (size == null) Assert.Ignore("AudioImporter.compSize not found in this Unity version");
            Assert.LessOrEqual(System.Convert.ToInt64(size.GetValue(importer)), 3_000_000L, file + " imported size");
        }

        [Test]
        public void MenuLobbyAndResultsUseTheUserLoopWithStreamingImport()
        {
            const string path = ArtAudioSpec.MusicFolder + "game_menu_looping.ogg";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var set = AssetDatabase.LoadAssetAtPath<AudioRuntimeSet>(RuntimeSetPath);
            Assert.IsNotNull(clip);
            foreach (var screen in new[] { JungleMaze.App.Flow.AppScreen.MainMenu,
                         JungleMaze.App.Flow.AppScreen.InRoom, JungleMaze.App.Flow.AppScreen.Results })
                Assert.AreSame(clip, set.MusicFor(MusicPlan.TrackFor(screen)), screen.ToString());
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType);
            Assert.IsTrue(importer.loadInBackground);
        }

        [Test]
        public void WaitingRoom_NoLongerBorrowsLoading()
        {
            var set = AssetDatabase.LoadAssetAtPath<AudioRuntimeSet>(RuntimeSetPath);
            Assert.AreNotSame(set.MusicFor(MusicTrack.Loading), set.MusicFor(MusicTrack.WaitingRoom));
        }
    }
}
