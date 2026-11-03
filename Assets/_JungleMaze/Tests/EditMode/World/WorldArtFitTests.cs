using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.World.Phase2;
using JungleMaze.World;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    /// <summary>World art keeps its aspect on any footprint: no stretched statues, walls tile, ground items show icons.</summary>
    public sealed class WorldArtFitTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private WorldVisualSet _set;
        private GameObject _object;
        private WorldObjectView _view;

        private SpriteRenderer Renderer => _object.GetComponent<SpriteRenderer>();

        [SetUp]
        public void SetUp()
        {
            _set = Own(ScriptableObject.CreateInstance<WorldVisualSet>());
            _object = Own(new GameObject("fit test"));
            _view = _object.AddComponent<WorldObjectView>();
            _view.Configure(_set);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [Test]
        public void TallStatueKeepsAspect_StandsOnFootprintBottom_AndIsCappedInHeight()
        {
            var tall = Art(50, 106);
            _set.Entries = new[] { new WorldVisualSet.Entry { Kind = WorldObjectKind.Statue, Sprite = tall } };
            _view.Apply(State(4, 3, 3), tall);
            var b = Renderer.bounds;
            Assert.AreEqual(50f / 106f, b.size.x / b.size.y, 1e-3f, "aspect");
            Assert.AreEqual(10f - 1.5f, b.min.y, 1e-3f, "stands on the footprint bottom");
            Assert.LessOrEqual(b.size.y, 3f * WorldObjectView.MaxRise + 1e-3f, "height cap");
            Assert.LessOrEqual(b.size.x, 3f + 1e-3f, "never wider than the footprint");
        }

        [Test]
        public void WideRockSpansFootprintWidthWithoutStretch()
        {
            var wide = Art(63, 50);
            _set.Entries = new[] { new WorldVisualSet.Entry { Kind = WorldObjectKind.Rock, Sprite = wide } };
            _view.Apply(State(3, 4, 4), wide);
            var b = Renderer.bounds;
            Assert.AreEqual(4f, b.size.x, 1e-3f);
            Assert.AreEqual(63f / 50f, b.size.x / b.size.y, 1e-3f);
        }

        [Test]
        public void FlatHazardFitsInsideFootprintCentred()
        {
            var lava = Art(80, 55);
            _set.Entries = new[] { new WorldVisualSet.Entry { Kind = WorldObjectKind.Lava, Sprite = lava } };
            _view.Apply(State(9, 3, 3), lava);
            var b = Renderer.bounds;
            Assert.AreEqual(3f, b.size.x, 1e-3f);
            Assert.AreEqual(80f / 55f, b.size.x / b.size.y, 1e-3f);
            Assert.AreEqual(new Vector3(10, 10, 0), _object.transform.position);
        }

        [Test]
        public void WallTilesAcrossItsFootprint()
        {
            var block = Art(16, 16, SpriteMeshType.FullRect);
            _set.Entries = new[] { new WorldVisualSet.Entry { Kind = WorldObjectKind.Wall, Sprite = block } };
            _view.Apply(State(2, 1, 4), block);
            Assert.AreEqual(SpriteDrawMode.Tiled, Renderer.drawMode);
            Assert.AreEqual(new Vector2(1, 4), Renderer.size);
            Assert.AreEqual(Vector3.one, _object.transform.localScale);
        }

        [Test]
        public void GroundItemShowsItsIconUntinted()
        {
            var icon = Art(32, 32);
            var square = Art(1, 1);
            var views = new Dictionary<ushort, WorldObjectView>();
            var root = Own(new GameObject("root")).transform;
            var item = State(100, .35f, .35f);
            item.ItemId = "KNIFE";
            WorldSceneView.ApplyViews(new[] { item }, root, views, _set, square, new Icons(icon));
            var renderer = views[item.Id].GetComponent<SpriteRenderer>();
            Assert.AreSame(icon, renderer.sprite);
            Assert.AreEqual(Color.white, renderer.color);
            Assert.AreEqual(.35f, renderer.bounds.size.x, 1e-3f);
        }

        [Test]
        public void GroundItemWithoutIconKeepsTheMarker()
        {
            var square = Art(1, 1);
            var views = new Dictionary<ushort, WorldObjectView>();
            var root = Own(new GameObject("root")).transform;
            var item = State(100, .35f, .35f);
            item.ItemId = "UNKNOWN";
            WorldSceneView.ApplyViews(new[] { item }, root, views, _set, square, new Icons(null));
            var renderer = views[item.Id].GetComponent<SpriteRenderer>();
            Assert.AreSame(square, renderer.sprite);
            Assert.AreEqual(WorldObjectView.ColorFor(100), renderer.color);
        }

        private sealed class Icons : IItemIconProvider
        {
            private readonly Sprite _icon;
            public Icons(Sprite icon) => _icon = icon;
            public Sprite GetIcon(string itemId) => itemId == "KNIFE" ? _icon : null;
        }

        private static WorldObjectWire State(byte kind, float width, float height) =>
            new WorldObjectWire { Id = 7, Kind = kind, X = 10, Y = 10, Width = width, Height = height, Hp = 30 };

        private Sprite Art(int w, int h, SpriteMeshType mesh = SpriteMeshType.Tight)
        {
            var texture = Own(new Texture2D(w, h));
            return Own(Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, 0f), 100, 0, mesh));
        }

        private T Own<T>(T o) where T : Object { _owned.Add(o); return o; }
    }
}
