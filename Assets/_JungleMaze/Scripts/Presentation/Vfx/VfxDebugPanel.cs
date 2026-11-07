#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Vfx;
using JungleMaze.App.Vfx.Mocks;
using JungleMaze.Core.DI;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.World.Throwables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// Dev-only VFX-01 panel, toggled with F10 and open by itself in the VfxPreview scene: buttons for every effect,
    /// repeats (dedupe), rollback, room change, fake fog-of-war per target and the 10-source load. A thin IMGUI layer
    /// over <see cref="VfxScenarios"/>; it never draws or decides anything itself. Not compiled into release builds.
    /// </summary>
    public sealed class VfxDebugPanel : MonoBehaviour
    {
        public const string PreviewSceneName = "VfxPreview";

        private VfxScenarios _scenarios;
        private VfxStage _stage;
        private IVfxTargetLocator _targets;
        private bool _visible;
        private Vector2 _scroll;
        private int _target = 1;
        private int _outcome = 1;

        private static readonly CombatOutcome[] Rejections =
        {
            CombatOutcome.Accepted, CombatOutcome.Cooldown, CombatOutcome.NoAmmo, CombatOutcome.Duplicate, CombatOutcome.RateLimited
        };

        [Inject]
        public void Construct(VfxScenarios scenarios, VfxStage stage, IVfxTargetLocator targets)
        {
            _scenarios = scenarios;
            _stage = stage;
            _targets = targets;
        }

        public bool Visible => _visible;

        private void Start() => _visible = SceneManager.GetActiveScene().name == PreviewSceneName;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _scenarios == null) return;
            var rect = new Rect(Screen.width - 330, 10, 320, Screen.height - 20);
            GUILayout.BeginArea(rect, GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("VFX-01 (F10) — dev only");
            var director = _scenarios.Director;
            GUILayout.Label($"Active {(_stage != null ? _stage.ActiveCount : 0)} · budget {director.LiveCount}/{director.BudgetCapacity} · refused {director.BudgetRefused}");
            GUILayout.Label($"Auras {director.AuraCount} · fuses {director.FuseCount} · resets {director.Resets}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target", GUILayout.Width(50));
            if (GUILayout.Button("-", GUILayout.Width(24))) _target = Mathf.Max(0, _target - 1);
            GUILayout.Label(_target == 0 ? "0 (own)" : _target.ToString(), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(24))) _target = Mathf.Min(10, _target + 1);
            GUILayout.EndHorizontal();
            var t = (byte)_target;

            GUILayout.Label("Hit (flash + number + spark)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hit 12")) _scenarios.Hit(t, 12);
            if (GUILayout.Button("Hit 45")) _scenarios.Hit(t, 45);
            if (GUILayout.Button("Repeat x2+1s")) _scenarios.RepeatHit(t);
            GUILayout.EndHorizontal();

            GUILayout.Label("Status auras");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stun 2s")) _scenarios.Status(t, StatusKind.Stun, 2f);
            if (GUILayout.Button("Reverse 10s")) _scenarios.Status(t, StatusKind.Reverse, 10f);
            if (GUILayout.Button("Invuln 2s")) _scenarios.Status(t, StatusKind.Invulnerable, 2f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Immune 7s")) _scenarios.Status(t, StatusKind.CcImmune, 7f);
            if (GUILayout.Button("Invisible 5s")) _scenarios.Status(t, StatusKind.Invisible, 5f);
            if (GUILayout.Button("Poison 3s")) _scenarios.Status(t, StatusKind.Poison, 3f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear stun")) _scenarios.ClearStatus(t, StatusKind.Stun);
            if (GUILayout.Button("Repeat stun")) _scenarios.RepeatStatus(t, StatusKind.Stun, 5f);
            GUILayout.EndHorizontal();

            GUILayout.Label("Throwables (fake WPN-03 events)");
            float x = 0f, y = 0f;
            if (_targets == null || !_targets.TryGetPosition(t, out x, out y)) { x = 0f; y = 0f; }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Banana")) _scenarios.Throw(ThrowableKind.Banana, x, y);
            if (GUILayout.Button("Smoke")) _scenarios.Throw(ThrowableKind.Smoke, x, y);
            if (GUILayout.Button("Seed")) _scenarios.Throw(ThrowableKind.Seed, x, y);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Meteorite")) _scenarios.Throw(ThrowableKind.Meteorite, x, y);
            if (GUILayout.Button("Crystal")) _scenarios.Throw(ThrowableKind.Crystal, x, y);
            if (GUILayout.Button("Trap step")) _scenarios.Throwables?.TriggerTrap(t);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bell (by 1)")) _scenarios.Throwables?.RingBell(1);
            if (GUILayout.Button("Bat stun")) _scenarios.Throwables?.BatHit(1, t, true);
            GUILayout.EndHorizontal();

            GUILayout.Label("Attack reconcile");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(24))) _outcome = (_outcome + Rejections.Length - 1) % Rejections.Length;
            GUILayout.Label(Rejections[_outcome].ToString(), GUILayout.Width(90));
            if (GUILayout.Button(">", GUILayout.Width(24))) _outcome = (_outcome + 1) % Rejections.Length;
            if (GUILayout.Button("Predict →")) _scenarios.Rollback(Rejections[_outcome]);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Remote attack (target)")) _scenarios.RemoteAttack(t);

            GUILayout.Label("Session / fog");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Change room")) _scenarios.ChangeRoom();
            var visibility = _scenarios.Visibility;
            if (visibility != null)
            {
                var hidden = visibility.IsHidden(t);
                if (GUILayout.Button(hidden ? "Show target" : "Hide target")) visibility.SetHidden(t, !hidden);
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button(_scenarios.LoadRunning ? "Stop load" : "Load: 10 sources 60s"))
            {
                if (_scenarios.LoadRunning) _scenarios.StopLoad();
                else _scenarios.StartLoad();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
#endif
