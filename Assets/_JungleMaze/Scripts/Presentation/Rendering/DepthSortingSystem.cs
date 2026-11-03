using System.Collections.Generic;
using UnityEngine;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// Y-sorting tập trung cho 2D top-down: chỉ entity được <see cref="MarkDirty"/> mới tính lại sortingOrder, gom
    /// về một LateUpdate duy nhất chạy sau mọi script khác (movement, Rigidbody2D interpolation, PlayerView, camera)
    /// nên luôn đọc vị trí cuối cùng của frame. Entity đứng yên không tốn gì. Một instance do Bootstrap tạo và đăng ký.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class DepthSortingSystem : MonoBehaviour
    {
        /// <summary>100 order/world unit: phân biệt 1cm, map 50 ô dùng khoảng [-5000, 0] trong giới hạn 16-bit.</summary>
        public const float DefaultPrecision = 100f;

        [SerializeField, Min(1f)] private float precision = DefaultPrecision;

        private readonly HashSet<DepthSortable> _registered = new HashSet<DepthSortable>();
        private readonly List<DepthSortable> _dirty = new List<DepthSortable>(64);

        public float Precision
        {
            get => precision;
            set => precision = Mathf.Max(1f, value);
        }

        public int RegisteredCount => _registered.Count;
        public int DirtyCount => _dirty.Count;

        /// <summary>sortingOrder = -round(y * precision), kẹp vào khoảng 16-bit mà SortingGroup/Renderer chấp nhận.</summary>
        public static int ComputeOrder(float worldY, float precision) =>
            Mathf.Clamp(-Mathf.RoundToInt(worldY * precision), short.MinValue, short.MaxValue);

        public bool IsRegistered(DepthSortable entity) => entity != null && _registered.Contains(entity);

        /// <summary>Đăng ký lần đầu xếp chỗ luôn (object tĩnh chỉ cần đúng một lần này). Gọi lặp lại không có tác dụng.</summary>
        public void Register(DepthSortable entity)
        {
            if (entity == null || !_registered.Add(entity)) return;
            MarkDirty(entity);
        }

        public void Unregister(DepthSortable entity)
        {
            if (ReferenceEquals(entity, null) || !_registered.Remove(entity)) return;
            if (!entity.IsQueued) return;
            entity.IsQueued = false;
            _dirty.Remove(entity);
        }

        /// <summary>Xếp lại entity này trong LateUpdate của frame. Gọi nhiều lần trong một frame chỉ xử lý một lần.</summary>
        public void MarkDirty(DepthSortable entity)
        {
            if (entity == null || entity.IsQueued || !_registered.Contains(entity)) return;
            entity.IsQueued = true;
            _dirty.Add(entity);
        }

        /// <summary>Public để test/tool gọi trực tiếp; bình thường chạy trong LateUpdate.</summary>
        public void ProcessDirty()
        {
            for (var i = 0; i < _dirty.Count; i++)
            {
                var entity = _dirty[i];
                if (entity == null) continue; // phòng hờ: Destroy luôn đi qua OnDisable → Unregister
                entity.IsQueued = false;
                entity.ApplySortingOrder(ComputeOrder(entity.SortPosition.y, precision));
            }
            _dirty.Clear();
        }

        private void LateUpdate()
        {
            if (_dirty.Count > 0) ProcessDirty();
        }
    }
}
