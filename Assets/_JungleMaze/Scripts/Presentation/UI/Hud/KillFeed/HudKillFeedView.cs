using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// Kill feed, top-left (T-022, mock B9, T-028 kit_killfeed_panel): rows "killer [weapon icon] victim", 24 px, 44 px
    /// apart; the panel widens with the text (max(270, row + 70) px) and hides when empty.
    /// NOT WIRED: the client has no all-players kill event yet (IEliminationSource is local-only). A future source
    /// calls <see cref="Push"/>; nothing in this view reads game state.
    /// </summary>
    public sealed class HudKillFeedView : MonoBehaviour
    {
        [Serializable]
        public struct Row
        {
            public RectTransform Root;
            public TMP_Text Killer;
            public Image Weapon;
            public TMP_Text Victim;
        }

        [SerializeField] private RectTransform panel;
        [SerializeField] private Row[] rows = new Row[3];
        [SerializeField] private float seconds = 5f;
        // Canvas units (1080p px / 1.5), set by the builder from the mock.
        [SerializeField] private float textLeft = 22f / 1.5f, iconGap = 12f / 1.5f, iconSize = 32f / 1.5f;
        [SerializeField] private float rowHeight = 44f / 1.5f, padTop = 11f / 1.5f, minWidth = 270f / 1.5f, extraWidth = 70f / 1.5f;

        private KillFeedLog<Sprite> _log;

        public RectTransform Panel => panel;
        public Row[] Rows => rows;
        public KillFeedLog<Sprite> Log => _log ?? (_log = new KillFeedLog<Sprite>(rows.Length, seconds));

        public void Push(string killer, Sprite weaponIcon, string victim)
        {
            Log.Push(killer, weaponIcon, victim);
            Layout();
        }

        public void Clear()
        {
            Log.Clear();
            Layout();
        }

        private void OnEnable() => Layout();

        private void Update()
        {
            if (Log.Count > 0 && Log.Tick(Time.deltaTime)) Layout();
        }

        /// <summary>Writes the rows and sizes the panel; runs only when the log changes.</summary>
        public void Layout()
        {
            var log = Log;
            if (panel != null && panel.gameObject.activeSelf != log.Count > 0) panel.gameObject.SetActive(log.Count > 0);
            var widest = 0f;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var shown = i < log.Count;
                if (row.Root.gameObject.activeSelf != shown) row.Root.gameObject.SetActive(shown);
                if (!shown) continue;
                var e = log[i];
                row.Killer.text = e.Killer;
                row.Victim.text = e.Victim;
                row.Weapon.sprite = e.Weapon;
                row.Weapon.enabled = e.Weapon != null;
                var killer = Mathf.Ceil(row.Killer.GetPreferredValues(e.Killer ?? "").x);
                var victim = Mathf.Ceil(row.Victim.GetPreferredValues(e.Victim ?? "").x);
                Place(row.Killer.rectTransform, textLeft, killer);
                Place(row.Weapon.rectTransform, textLeft + killer + iconGap, iconSize);
                Place(row.Victim.rectTransform, textLeft + killer + iconGap + iconSize + iconGap, victim);
                widest = Mathf.Max(widest, killer + iconGap + iconSize + iconGap + victim);
            }
            if (panel != null && log.Count > 0)
                panel.sizeDelta = new Vector2(Mathf.Max(minWidth, widest + extraWidth), padTop * 2f + rowHeight * log.Count);
        }

        private static void Place(RectTransform t, float x, float width)
        {
            t.anchoredPosition = new Vector2(x, t.anchoredPosition.y);
            t.sizeDelta = new Vector2(width, t.sizeDelta.y);
        }
    }
}
