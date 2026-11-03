using JungleMaze.Core.DI;
using UnityEngine;
using UnityEngine.Rendering;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// Đặt ở root của entity cần Y-sorting (Player, remote player, obstacle...). SortingGroup ở root gom mọi
    /// SpriteRenderer con (thân, vũ khí cầm tay) thành một khối; <see cref="DepthSortingSystem"/> chỉ ghi
    /// sortingOrder của group. Component này không có Update: ai di chuyển entity thì gọi <see cref="MarkDirty"/>.
    /// Phần cần sort độc lập (tán cây, mái nhà) phải nằm ngoài group này, xem Docs/hvu/05_Depth_Sorting.vi.md.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SortingGroup))]
    public sealed class DepthSortable : MonoBehaviour
    {
        [Tooltip("Điểm chạm đất (chân, gốc cây, đáy đá). Trống: dùng Transform root. Không dùng tâm sprite.")]
        [SerializeField] private Transform sortPoint;
        [Tooltip("Trống: lấy SortingGroup trên chính GameObject này.")]
        [SerializeField] private SortingGroup sortingGroup;

        private DepthSortingSystem _system;
        private int _appliedOrder;
        private bool _hasGroup;

        /// <summary>Do DepthSortingSystem quản lý: entity đang nằm trong dirty queue của frame.</summary>
        internal bool IsQueued { get; set; }

        public Transform SortPoint => sortPoint != null ? sortPoint : transform;
        public Vector3 SortPosition => SortPoint.position;
        public SortingGroup SortingGroup { get { EnsureGroup(); return sortingGroup; } }
        public int SortingOrder { get { EnsureGroup(); return _appliedOrder; } }

        [Inject]
        public void Construct(DepthSortingSystem system)
        {
            if (_system == system) return;
            if (_system != null) _system.Unregister(this);
            _system = system;
            // Scene object được inject sau OnEnable; object từ pool được inject trước OnEnable (khi đó chưa active).
            if (_system != null && isActiveAndEnabled) _system.Register(this);
        }

        /// <summary>Báo vị trí depth vừa đổi. Rẻ, gọi mỗi frame cũng được: trùng lặp bị loại trong system.</summary>
        public void MarkDirty()
        {
            if (_system != null) _system.MarkDirty(this);
        }

        /// <summary>Chỉ ghi SortingGroup khi giá trị thực sự đổi. Trả về true nếu đã ghi.</summary>
        public bool ApplySortingOrder(int order)
        {
            EnsureGroup();
            if (order == _appliedOrder) return false;
            _appliedOrder = order;
            sortingGroup.sortingOrder = order;
            return true;
        }

        private void Awake() => EnsureGroup();

        private void OnEnable()
        {
            if (_system != null) _system.Register(this);
        }

        private void OnDisable()
        {
            if (_system != null) _system.Unregister(this);
        }

        private void EnsureGroup()
        {
            if (_hasGroup) return;
            if (sortingGroup == null) sortingGroup = GetComponent<SortingGroup>();
            _appliedOrder = sortingGroup.sortingOrder;
            _hasGroup = true;
        }
    }
}
