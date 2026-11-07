#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using JungleMaze.Presentation.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>
    /// Huy 07/11: pressing a K2 button swapped the sprite to the pressed face but the icon and label stayed put. Every kit
    /// button in the built UI prefabs must carry <see cref="KitButtonFace"/>; pressed, its content sinks by the base
    /// (11 % of the height, ~7 px at 1080p on a 64 px button); released it returns; disabled it never moves.
    /// </summary>
    public sealed class KitButtonFaceTests
    {
        private static readonly string[] Prefabs =
        {
            "Assets/_JungleMaze/Prefabs/UI/Lobby/LobbyUi.prefab",
            "Assets/_JungleMaze/Prefabs/UI/MatchOverlay/MatchOverlay.prefab",
            "Assets/_JungleMaze/Prefabs/UI/Hud.prefab",
        };

        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _objects) if (o != null) Object.Destroy(o);
            _objects.Clear();
        }

        private static bool IsKit(Button b)
        {
            var img = b.targetGraphic as Image;
            var n = img != null && img.sprite != null ? img.sprite.name : "";
            return n.StartsWith("kit_btn_") || n.StartsWith("kit_close_") || n.StartsWith("kit_iconbtn_");
        }

        private static float CentreY(RectTransform t)
        {
            var c = new Vector3[4];
            t.GetWorldCorners(c);
            return (c[0].y + c[1].y) / 2f;
        }

        [UnityTest]
        public IEnumerator EveryKitButton_PressedSinksContentByBase_ReleasedReturns_DisabledStill()
        {
            var checkedButtons = 0;
            foreach (var path in Prefabs)
            {
                var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                _objects.Add(go);
                var canvas = go.GetComponent<Canvas>();
                if (canvas != null) canvas.enabled = true;
                foreach (Transform child in go.transform) child.gameObject.SetActive(true);
                foreach (var g in go.GetComponentsInChildren<CanvasGroup>(true)) { g.interactable = true; g.alpha = 1f; } // screens are swapped by CanvasGroup; a locked group reads as disabled
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (var b in go.GetComponentsInChildren<Button>(true))
                {
                    if (!IsKit(b)) continue;
                    var face = b.GetComponent<KitButtonFace>();
                    Assert.IsNotNull(face, path + ": " + b.name + " is a kit button without KitButtonFace");
                    Assert.IsNotNull(face.Content, b.name + " face content");
                    if (!b.gameObject.activeInHierarchy) continue;
                    b.interactable = true;
                    face.SendMessage("LateUpdate");
                    var rest = CentreY(face.Content);
                    var unitsToWorld = ((RectTransform)b.transform).lossyScale.y;
                    face.OnPointerDown(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                    Assert.That(rest - CentreY(face.Content), Is.EqualTo(face.Base * unitsToWorld).Within(0.02f * unitsToWorld + 1e-4f),
                        path + ": " + b.name + " content sinks by the base while pressed");
                    Assert.IsTrue(face.Pressed);
                    face.OnPointerUp(new PointerEventData(null));
                    Assert.That(CentreY(face.Content), Is.EqualTo(rest).Within(1e-3f), b.name + " returns on release");
                    b.interactable = false;
                    face.SendMessage("LateUpdate");
                    var disabled = CentreY(face.Content);
                    face.OnPointerDown(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                    face.SendMessage("LateUpdate");
                    Assert.That(CentreY(face.Content), Is.EqualTo(disabled).Within(1e-3f), b.name + " disabled never moves");
                    face.OnPointerUp(new PointerEventData(null));
                    b.interactable = true;
                    checkedButtons++;
                }
            }
            Assert.Greater(checkedButtons, 10, "kit buttons found in the prefabs");
        }

        [Test]
        public void Base_IsElevenPercentOfHeight_SevenPxOnA64PxButton()
        {
            var go = new GameObject("b", typeof(RectTransform));
            _objects.Add(go);
            ((RectTransform)go.transform).sizeDelta = new Vector2(100f, 64f / 1.5f);
            var face = go.AddComponent<KitButtonFace>();
            Assert.That(face.Base * 1.5f, Is.EqualTo(7.04f).Within(0.05f));
        }
    }
}
#endif
