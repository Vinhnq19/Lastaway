using System.Collections.Generic;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using UnityEngine;

namespace JungleMaze.Presentation.World
{
    /// <summary>
    /// Collider hard cover của map đang nạp (map_demo offline hoặc map phòng): mỗi cover một BoxCollider2D trên layer
    /// HardCover, tên = tên cover (đúng điều MatchDemoView.VerifyGeometry kiểm). Dựng lại khi dữ liệu map đổi, nên đổi map
    /// không cần bake lại scene. Collider bake sẵn dưới object này được thay thế.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapCoverColliders : MonoBehaviour
    {
        private IClientRuntimeData _runtime;
        private IReadOnlyList<RuntimeCover> _built;

        [Inject]
        public void Construct(IClientRuntimeData runtime) => _runtime = runtime;

        private void Start() => Sync();

        private void Update() => Sync();

        private void Sync()
        {
            var covers = _runtime?.Covers;
            if (covers == null || covers.Count == 0 || ReferenceEquals(covers, _built)) return;
            _built = covers;
            for (var i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            var layer = LayerMask.NameToLayer("HardCover");
            foreach (var cover in covers)
            {
                var go = new GameObject(cover.Name) { layer = layer };
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3((cover.MinX + cover.MaxX) / 2f, (cover.MinY + cover.MaxY) / 2f, 0f);
                go.AddComponent<BoxCollider2D>().size = new Vector2(cover.MaxX - cover.MinX, cover.MaxY - cover.MinY);
            }
        }
    }
}
