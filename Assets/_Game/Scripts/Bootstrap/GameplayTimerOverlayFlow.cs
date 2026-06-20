using System;
using FlowBlast.Services.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowBlast.Bootstrap
{
    public sealed class GameplayTimerOverlayFlow : IDisposable
    {
        private const string OverlayRootName = "GameplayTimerOverlay";
        private const string TimerTextName = "TimerText";
        private const float PanelWidth = 220f;
        private const float PanelHeight = 72f;
        private const float TopOffset = -36f;
        private const int OverlaySortingOrder = 100;

        private readonly LevelController levelController;
        private readonly GameObject overlayRoot;
        private readonly TMP_Text timerText;

        public GameplayTimerOverlayFlow(Transform parent, LevelController levelController)
        {
            this.levelController = levelController;
            overlayRoot = CreateOverlay(parent, out timerText);

            if (this.levelController != null)
            {
                this.levelController.OnRemainingTimeChanged += OnRemainingTimeChanged;
                OnRemainingTimeChanged(Mathf.CeilToInt(this.levelController.RemainingTimeSeconds));
            }
        }

        public void Dispose()
        {
            if (levelController != null)
            {
                levelController.OnRemainingTimeChanged -= OnRemainingTimeChanged;
            }

            if (overlayRoot != null)
            {
                UnityEngine.Object.Destroy(overlayRoot);
            }
        }

        private void OnRemainingTimeChanged(int remainingSeconds)
        {
            if (timerText == null)
            {
                return;
            }

            int safeSeconds = Mathf.Max(0, remainingSeconds);
            int minutes = safeSeconds / 60;
            int seconds = safeSeconds % 60;
            timerText.text = $"TIME {minutes:00}:{seconds:00}";
        }

        private static GameObject CreateOverlay(Transform parent, out TMP_Text text)
        {
            GameObject root = new GameObject(OverlayRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject panel = new GameObject("TimerPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchoredPosition = new Vector2(0f, TopOffset);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject textObject = new GameObject(TimerTextName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 40f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = "TIME 01:00";

            text = tmp;
            return root;
        }
    }
}
