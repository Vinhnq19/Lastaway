using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Combat.Mocks;
using JungleMaze.App.Match;
using JungleMaze.App.MatchUi;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Pooling;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using JungleMaze.Domain.Balance;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.World;
using JungleMaze.App.World;
using JungleMaze.App.Flow;
using JungleMaze.World.Throwables;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Weapon input and pooled visuals. Online play uses the authoritative combat session;
    /// the in-process backend is retained only for disconnected local previews.
    /// </summary>
    public sealed class CombatWeaponPresenter : MonoBehaviour
    {
        private const byte LocalNetId = 0;

        private static readonly KeyCode[] SelectKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };

        [SerializeField] private Transform hero;
        [SerializeField] private CoverProbeProjectile projectilePrefab;
        [SerializeField] private MeleeSwingVisual swingPrefab;
        /// <summary>Index = CombatWeaponId. Entry 0 (Hand) may stay empty; weapon sprites are read from these prefabs.</summary>
        [SerializeField] private GameObject[] weaponPrefabs = new GameObject[CombatWeaponIds.Count];
        /// <summary>Remote bodies to animate on confirmed attacks; found in the scene when left empty.</summary>
        [SerializeField] private RemotePlayerSpawner remotePlayers;
        /// <summary>Per-weapon shot art; empty keeps the plain tracer for every ranged weapon.</summary>
        [SerializeField] private ProjectileArtCatalog projectileArt;
        /// <summary>Optional muzzle flash prefab for weapons whose art has a muzzle clip.</summary>
        [SerializeField] private ShotFlashVisual flashPrefab;

        private PooledPrefabFactory<ShotFlashVisual> _flashes;
        /// <summary>Distance from the wielder's centre to the barrel when no in-hand weapon is drawn.</summary>
        private const float MuzzleReach = .45f;

        private HeroSpriteAnimator _heroAnimator;
        private HeldWeaponView _heroWeapon;
        private HeldWeaponCatalog _heldCatalog;

        private IBalanceConfigProvider _balance;
        private IPoolService _pool;
        private IHudStateWriter _hud;
        private FakeCombatBackend _server;
        private MatchSession _match;
        private OnlineCombatSession _online;
        private IMatchMenuState _menu;
        private IPlayerPoseSource _poses;
        private WeaponController _controller;
        private PooledPrefabFactory<CoverProbeProjectile> _projectiles;
        private PooledPrefabFactory<MeleeSwingVisual> _swings;
        private readonly Sprite[] _weaponSprites = new Sprite[CombatWeaponIds.Count];
        private readonly float[] _pelletAngles = new float[16];
        private System.Action<CombatEvent> _onChanged;
        private float _aimDegrees;
        private float _serverTickAccumulator;
        private OfflineWorldSession _offlineWorld;
        private OfflineMatchSession _offlineMatch;
        private GameFlow _flow;
        private OnlineThrowableSession _throwables;
        private ThrowableView _throwView;
        private ReloadProgressView _reloadProgress;

        [Inject]
        public void Construct(IBalanceConfigProvider balance, IPoolService pool, IHudStateWriter hud,
            MatchSession match, OnlineCombatSession online, IMatchMenuState menu, IPlayerPoseSource poses)
        {
            _balance = balance;
            _pool = pool;
            _hud = hud;
            _match=match; _online=online; _menu=menu; _poses=poses;
        }

        [Inject]
        public void ConfigureOffline(OfflineWorldSession offlineWorld, OfflineMatchSession offlineMatch, GameFlow flow)
        {
            _offlineWorld = offlineWorld;
            _offlineMatch = offlineMatch;
            _flow = flow;
        }

        /// <summary>WPN-03 thrown and special weapons in networked matches; optional for tools and tests.</summary>
        [Inject] public void ConfigureThrowables(OnlineThrowableSession throwables) => _throwables = throwables;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Only development previews receive a fake backend; release online injection never requires it.</summary>
        [Inject] public void ConfigurePreview(FakeCombatBackend server) => _server=server;
#endif

        private void Start()
        {
            if (hero == null || projectilePrefab == null || swingPrefab == null || _pool == null || _balance == null)
            {
                enabled = false;
                return;
            }
            _projectiles = new PooledPrefabFactory<CoverProbeProjectile>(_pool, projectilePrefab, 16);
            _swings = new PooledPrefabFactory<MeleeSwingVisual>(_pool, swingPrefab, 4);
            _heroAnimator = hero.GetComponentInChildren<HeroSpriteAnimator>(true);
            _heroWeapon = hero.GetComponentInChildren<HeldWeaponView>(true);
            _heldCatalog = _heroWeapon != null ? _heroWeapon.Catalog : null;
            if (remotePlayers == null) remotePlayers = FindAnyObjectByType<RemotePlayerSpawner>();
            if (flashPrefab != null) _flashes = new PooledPrefabFactory<ShotFlashVisual>(_pool, flashPrefab, 4);
            for (var i = 0; i < weaponPrefabs.Length && i < _weaponSprites.Length; i++)
            {
                if (weaponPrefabs[i] == null) continue;
                var renderer = weaponPrefabs[i].GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null) _weaponSprites[i] = renderer.sprite;
            }

            _throwView = gameObject.AddComponent<ThrowableView>();
            _throwView.Bind(hero, remotePlayers, _match, _throwables, _offlineWorld);
            if (!_match.Connected && _server!=null) CreateOfflineController();
            _online.Changed+=OnCombatEvent;
            if (_offlineWorld != null) _offlineWorld.CombatChanged += OnCombatEvent;
        }

        private void CreateOfflineController()
        {
            var peer = _server.AddPeer(LocalNetId);
            _controller = new WeaponController(_balance, peer, _hud, LocalNetId);
            peer.AttackResult += _controller.OnAttackResult;
            peer.ReloadResult += _controller.OnReloadResult;
            _onChanged = OnCombatEvent;
            _controller.Changed += _onChanged;
            _controller.Select(CombatWeaponId.Hand);
        }

        private void Update()
        {
            if (_online==null || _pool==null || _projectiles==null) return;
            if (_match.Connected)
            {
                if (HandleThrowable(true)) return;
                if (!_online.CanAct || (_menu?.BlocksGameplayInput??false)) return;
                if (Input.GetKeyDown(KeyCode.R)) _online.TryReload();
                if (WantsFire(_online.Controller))
                {
                    if (HeroAim.TryGetTargetDegrees(Camera.main, hero.position, Input.mousePosition, out var onlineAim)) _aimDegrees=onlineAim;
                    _online.TryAttack(_aimDegrees);
                }
                return;
            }
            if (_flow != null && _flow.IsOffline && _offlineWorld != null)
            {
                if (_offlineMatch != null && _offlineMatch.IsDead) return;
                if (_menu?.BlocksGameplayInput ?? false) return;
                if (HandleThrowable(false)) return;
                if (Input.GetKeyDown(KeyCode.R)) _offlineWorld.TryReload();
                if (WantsFire(_offlineWorld.Combat?.Controller))
                {
                    if (HeroAim.TryGetTargetDegrees(Camera.main, hero.position, Input.mousePosition, out var offlineAim)) _aimDegrees = offlineAim;
                    _offlineWorld.TryAttack(_aimDegrees);
                }
                return;
            }
            if (_server==null)return;
            if (_controller == null) CreateOfflineController();

            // Stand-in for the server: answer requests from the previous frame, then advance ticks at 30 Hz.
            _server.Flush();
            _serverTickAccumulator += Time.deltaTime;
            var step = 1f / CombatShotMath.TickRate;
            while (_serverTickAccumulator >= step)
            {
                _serverTickAccumulator -= step;
                _server.AdvanceTicks(1);
            }

            for (var i = 0; i < SelectKeys.Length; i++)
            {
                if (Input.GetKeyDown(SelectKeys[i])) _controller.Select((CombatWeaponId)i);
            }
            if (Input.GetKeyDown(KeyCode.R)) _controller.TryReload();

            if (WantsFire(_controller))
            {
                if (HeroAim.TryGetTargetDegrees(Camera.main, hero.position, Input.mousePosition, out var degrees)) _aimDegrees = degrees;
                _controller.TryAttack(_aimDegrees);
            }
            _controller.Tick(Time.deltaTime);
        }

        /// <summary>
        /// WPN-03 input: with a thrown or special weapon in hand, a left click uses it toward the cursor (thrown items
        /// land at the cursor, clamped by the server). Returns true when a throwable is held, so the normal weapon
        /// path does not also fire.
        /// </summary>
        private static bool WantsFire(WeaponController controller) => controller != null &&
            (controller.SelectedSlot.SemiAutomatic ? Input.GetMouseButtonDown(0) : Input.GetMouseButton(0));

        private bool HandleThrowable(bool online)
        {
            var kind = ThrowableRules.KindOf(_heroWeapon != null ? _heroWeapon.ItemId : null);
            if (kind == ThrowableKind.None) return false;
            if ((_menu?.BlocksGameplayInput ?? false) || !Input.GetMouseButtonDown(0)) return true;
            var camera = Camera.main;
            if (camera == null || !HeroAim.TryGetTargetDegrees(camera, hero.position, Input.mousePosition, out var aim)) return true;
            var cursor = camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -camera.transform.position.z));
            var distance = Vector2.Distance(hero.position, cursor);
            if (online) _throwables?.TryUse(kind, aim, distance);
            else _offlineWorld?.TryUseThrowable(aim, distance);
            return true;
        }

        private void LateUpdate()
        {
            WeaponController controller = null;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game" &&
                !(_menu?.BlocksGameplayInput ?? false))
            {
                if (_match?.Connected == true && _online?.CanAct == true)
                    controller = _online.Controller;
                else if (_flow?.IsOffline == true && _offlineWorld?.IsReady == true &&
                         _offlineMatch?.IsDead != true)
                    controller = _offlineWorld.Combat?.Controller;
            }
            if (hero == null || controller == null || !controller.IsReloading)
            {
                if (_reloadProgress != null) _reloadProgress.Hide();
                return;
            }
            if (_reloadProgress == null) _reloadProgress = gameObject.AddComponent<ReloadProgressView>();
            var duration = Mathf.Max(.001f, controller.SelectedSlot.ReloadSeconds);
            var progress = Mathf.Clamp01(1f - controller.ReloadRemainingSeconds / duration);
            // A delayed confirmation must never look like a completed reload.
            if (!controller.IsReloadConfirmed) progress = Mathf.Min(progress, .95f);
            _reloadProgress.Show(LocalVisual(out _, out _), progress);
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.Changed -= _onChanged;
            if (_online!=null) _online.Changed-=OnCombatEvent;
            if (_offlineWorld != null) _offlineWorld.CombatChanged -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Kind == CombatEventKind.AttackPredicted)
            {
                var self = LocalVisual(out var body, out var hands);
                var swing = SpawnAttackVisual(combatEvent.Weapon, combatEvent.AimDegrees, self.position, self, hands, out var arc);
                Animate(body, hands, combatEvent.Weapon, swing, arc);
            }
            else if (combatEvent.Kind == CombatEventKind.RemoteAttackConfirmed && _poses!=null && _poses.TryGetPose(combatEvent.NetId,out var pose))
            {
                var view = remotePlayers != null ? remotePlayers.GetView(combatEvent.NetId) : null;
                var held = view != null ? view.GetComponentInChildren<HeldWeaponView>() : null;
                if (held != null) held.Show(CombatWeaponIds.ToJsonId(combatEvent.Weapon));
                var swing = SpawnAttackVisual(combatEvent.Weapon, pose.RotationDegrees, new Vector3(pose.X,pose.Y,0), null, held, out var arc);
                if (view != null)
                    Animate(view.GetComponentInChildren<HeroSpriteAnimator>(), held, combatEvent.Weapon, swing, arc);
            }
            else if (combatEvent.Kind == CombatEventKind.ReloadStarted)
            {
                LocalVisual(out _, out var hands);
                AnimateReload(hands, combatEvent.Weapon);
            }
            else if (combatEvent.Kind == CombatEventKind.RemoteReloadStarted)
            {
                var view = remotePlayers != null ? remotePlayers.GetView(combatEvent.NetId) : null;
                if (view != null) AnimateReload(view.GetComponentInChildren<HeldWeaponView>(), combatEvent.Weapon);
            }
        }

        /// <summary>
        /// The character the player sees as themselves. Offline that is the Hero; online the Hero is a hidden camera
        /// proxy and the own PlayerView draws the player, so attacks must animate that view's body and hands.
        /// </summary>
        private Transform LocalVisual(out HeroSpriteAnimator body, out HeldWeaponView hands)
        {
            if (_match != null && _match.Connected && _match.OwnNetId != 255 && remotePlayers != null)
            {
                var view = remotePlayers.GetView(_match.OwnNetId);
                if (view != null && view.isActiveAndEnabled)
                {
                    body = view.GetComponentInChildren<HeroSpriteAnimator>();
                    hands = view.GetComponentInChildren<HeldWeaponView>();
                    return view.transform;
                }
            }
            body = _heroAnimator;
            hands = _heroWeapon;
            return hero;
        }

        /// <summary>Dips the held weapon for the weapon's reload time. Melee and unknown weapons have no reload.</summary>
        private void AnimateReload(HeldWeaponView held, CombatWeaponId weapon)
        {
            if (held == null) return;
            var controller = ActiveController();
            if (controller == null) return;
            var slot = controller.Slot(weapon);
            if (slot.IsRanged) held.OnReload(slot.ReloadSeconds);
        }

        private WeaponController ActiveController() =>
            _match.Connected ? _online.Controller : (_flow != null && _flow.IsOffline && _offlineWorld != null ? _offlineWorld.Combat?.Controller : _controller);

        /// <summary>Body attack swing plus in-hand weapon: remote hands switch to the weapon they attacked with.</summary>
        private void Animate(HeroSpriteAnimator body, HeldWeaponView held, CombatWeaponId weapon, float swingSeconds, float arcDegrees)
        {
            if (swingSeconds < 0f) return;
            if (body != null) body.PlayAttack(BodyAttackSeconds(weapon, swingSeconds));
            if (held == null) return;
            held.Show(CombatWeaponIds.ToJsonId(weapon));
            held.OnAttack(swingSeconds > 0f, swingSeconds, arcDegrees);
        }

        /// <summary>
        /// How long the body attack pose lasts: a melee swing matches the hand swing (same 0.12 s floor as
        /// <see cref="HeldWeaponView.OnAttack"/>); a shot is a short release that never outlasts the weapon's cadence.
        /// </summary>
        private float BodyAttackSeconds(CombatWeaponId weapon, float swingSeconds)
        {
            if (swingSeconds > 0f) return Mathf.Max(.12f, swingSeconds);
            var controller = ActiveController();
            var cadence = controller != null ? controller.Slot(weapon).CadenceSeconds : .4f;
            return Mathf.Clamp(cadence * .6f, .12f, .3f);
        }

        /// <summary>
        /// Spawns the projectile, or the melee swing when the wielder has no in-hand view (a wielder with one swings
        /// its own weapon, or punches bare-handed). Returns the swing length (0 for ranged, -1 when nothing was shown).
        /// </summary>
        private float SpawnAttackVisual(CombatWeaponId weapon, float aimDegrees, Vector3 origin, Transform anchor, HeldWeaponView wielder, out float arcDegrees)
        {
            arcDegrees = 0f;
            var controller = ActiveController();
            if (controller==null) return -1f;
            var slot = controller.Slot(weapon);
            arcDegrees = slot.ArcDegrees;
            if (slot.IsRanged)
            {
                var itemId = CombatWeaponIds.ToJsonId(weapon);
                var shot = new ConfiguredShot(_balance, itemId);
                var count = CombatShotMath.PelletAngles(aimDegrees, slot.Pellets, slot.SpreadDegrees, _pelletAngles);
                var visualOrigin = origin;
                if (weapon >= CombatWeaponId.Pistol && wielder != null && wielder.Visible)
                {
                    var muzzle = wielder.MuzzlePosition;
                    var offset = (Vector2)(muzzle - origin);
                    var wall = CoverQuery.Sweep(origin, offset.normalized, offset.magnitude, .1f);
                    visualOrigin = wall.collider != null ? (Vector3)wall.centroid : muzzle;
                }
                var pose = new SpawnPose(visualOrigin, Quaternion.identity);
                var art = default(ProjectileArtCatalog.Entry);
                var hasArt = projectileArt != null && projectileArt.TryGet(itemId, out art);
                for (var i = 0; i < count; i++)
                {
                    var radians = _pelletAngles[i] * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                    var projectile = _projectiles.Create(in pose);
                    if (hasArt) projectile.SetLook(art.HasFlight ? art.Flight : null, art.FlightScale, art.AngleOffset, art.Spin, art.TracerColor);
                    projectile.Launch(in shot, direction, Vector3.Distance(origin, visualOrigin));
                }
                if (hasArt && art.HasMuzzle && _flashes != null)
                {
                    var aim = aimDegrees * Mathf.Deg2Rad;
                    var flash = _flashes.Create(SpawnPose.At(origin));
                    var muzzle = wielder != null && wielder.Visible ? wielder.MuzzlePosition : origin + new Vector3(Mathf.Cos(aim), Mathf.Sin(aim), 0f) * MuzzleReach;
                    flash.Play(art.Muzzle, muzzle, aimDegrees, art.MuzzleScale);
                    _pool.Despawn(flash, flash.Duration + .05f);
                }
                if (hasArt && art.HasCasing && _flashes != null)
                {
                    var casing = _flashes.Create(SpawnPose.At(visualOrigin));
                    casing.Eject(art.Casing, wielder != null ? wielder.MainHandPosition : origin, aimDegrees, art.CasingScale);
                    _pool.Despawn(casing, casing.Duration + .05f);
                }
                return 0f;
            }

            var duration = Mathf.Max(0.1f, slot.CadenceSeconds * 0.6f);
            if (wielder != null) return duration;
            var swing = _swings.Create(SpawnPose.At(origin));
            if (_heldCatalog != null && _heldCatalog.TryGet(CombatWeaponIds.ToJsonId(weapon), out var held))
                swing.Play(held.Sprite, anchor, aimDegrees, slot.ArcDegrees, slot.Range, duration, held.Scale, held.AngleOffset);
            else
                swing.Play(_weaponSprites[(int)weapon], anchor, aimDegrees, slot.ArcDegrees, slot.Range, duration);
            _pool.Despawn(swing, duration + 0.05f);
            return duration;
        }
    }
}
