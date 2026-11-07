#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JungleMaze.App.Match;
using JungleMaze.App.Vfx.Mocks;
using JungleMaze.Core.DI;
using JungleMaze.Presentation.Combat;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// Dev-only rig of the VfxPreview scene (plan 4.12.5): turns the mock match on and adds a real
    /// <see cref="ThrowableView"/> fed by the same <see cref="FakeThrowableEventSource"/> as the VFX adapter, so one fake
    /// WPN-03 event shows both layers. Only calls the public API of <c>ThrowableView</c>. Not compiled into release builds.
    /// </summary>
    public sealed class VfxPreviewRig : MonoBehaviour
    {
        [Tooltip("Dummy drawn as the own player (net id 0); thrown items start from it.")]
        [SerializeField] private Transform localDummy;

        private MockVfxMatch _mock;
        private FakeThrowableEventSource _throwables;
        private MatchSession _match;
        private ThrowableView _view;

        public ThrowableView ThrowableView => _view;

        [Inject]
        public void Construct(MockVfxMatch mock, FakeThrowableEventSource throwables, MatchSession match)
        {
            _mock = mock;
            _throwables = throwables;
            _match = match;
            Setup();
        }

        /// <summary>For tests that build the rig without the composition root.</summary>
        public void Construct(MockVfxMatch mock, FakeThrowableEventSource throwables, MatchSession match, Transform local)
        {
            localDummy = local;
            Construct(mock, throwables, match);
        }

        private void Setup()
        {
            if (_mock != null)
            {
                _mock.Active = true;
                _mock.InGame = true;
                _mock.OwnNetId = 0;
                _mock.Online = false;
            }
            if (_view == null && _throwables != null)
            {
                _view = gameObject.AddComponent<ThrowableView>();
                _view.Bind(localDummy, null, _match, _throwables);
            }
        }

        private void OnDestroy()
        {
            if (_mock != null) _mock.Active = false;
        }
    }
}
#endif
