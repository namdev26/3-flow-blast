using System;
using FlowBlast.Core.Events;
using FlowBlast.Core.Utilities;
using FlowBlast.Services.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowBlast.Bootstrap
{
    public sealed class RevivePopupFlow : IDisposable
    {
        private const string CountdownTextObjectName = "CountdownText";
        private const string AdReviveButtonObjectName = "AdReviveButton";
        private const string GemReviveButtonObjectName = "GemReviveButton";
        private const string DeclineButtonObjectName = "DeclineButton";

        private readonly IGameEventBus eventBus;
        private readonly Action onReviveRequested;
        private readonly Action onDeclineRequested;
        private readonly GameObject popupRoot;
        private readonly LevelController levelController;
        private readonly TMP_Text countdownText;
        private readonly Button adReviveButton;
        private readonly Button gemReviveButton;
        private readonly Button declineButton;

        public RevivePopupFlow(
            IGameEventBus eventBus,
            GameObject popupRoot,
            LevelController levelController,
            Action onReviveRequested,
            Action onDeclineRequested)
        {
            this.eventBus = eventBus;
            this.popupRoot = popupRoot;
            this.levelController = levelController;
            this.onReviveRequested = onReviveRequested;
            this.onDeclineRequested = onDeclineRequested;

            countdownText = ResolveText(popupRoot, CountdownTextObjectName);
            adReviveButton = ResolveButton(popupRoot, AdReviveButtonObjectName);
            gemReviveButton = ResolveButton(popupRoot, GemReviveButtonObjectName);
            declineButton = ResolveButton(popupRoot, DeclineButtonObjectName);

            Hide();
            RegisterEvents();

            if (this.levelController != null)
            {
                this.levelController.OnReviveCountdownChanged += OnReviveCountdownChanged;
                this.levelController.OnReviveExpired += OnReviveExpired;
            }
        }

        public void Dispose()
        {
            eventBus?.Unsubscribe<LevelLostEvent>(OnLevelLost);

            if (levelController != null)
            {
                levelController.OnReviveCountdownChanged -= OnReviveCountdownChanged;
                levelController.OnReviveExpired -= OnReviveExpired;
            }

            if (adReviveButton != null)
            {
                adReviveButton.onClick.RemoveListener(OnReviveButtonClicked);
            }

            if (gemReviveButton != null)
            {
                gemReviveButton.onClick.RemoveListener(OnReviveButtonClicked);
            }

            if (declineButton != null)
            {
                declineButton.onClick.RemoveListener(OnDeclineButtonClicked);
            }
        }

        public void SetCountdown(int seconds)
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.text = Mathf.Max(0, seconds).ToString();
        }

        private void RegisterEvents()
        {
            eventBus?.Subscribe<LevelLostEvent>(OnLevelLost);

            if (adReviveButton != null)
            {
                adReviveButton.onClick.AddListener(OnReviveButtonClicked);
            }

            if (gemReviveButton != null)
            {
                gemReviveButton.onClick.AddListener(OnReviveButtonClicked);
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(OnDeclineButtonClicked);
            }
        }

        private void OnLevelLost(LevelLostEvent gameEvent)
        {
            if (!gameEvent.CanRevive)
            {
                return;
            }

            SetCountdown(gameEvent.ReviveCountdownSeconds);
            Show();
        }

        private void OnReviveCountdownChanged(int remainingSeconds)
        {
            SetCountdown(remainingSeconds);
        }

        private void OnReviveExpired()
        {
            Hide();
            onDeclineRequested?.Invoke();
        }

        private void OnReviveButtonClicked()
        {
            Hide();
            onReviveRequested?.Invoke();
        }

        private void OnDeclineButtonClicked()
        {
            Hide();
            levelController?.DeclineRevive();
            onDeclineRequested?.Invoke();
        }

        private void Show()
        {
            if (popupRoot == null)
            {
                return;
            }

            popupRoot.SetActive(true);
        }

        private void Hide()
        {
            if (popupRoot == null)
            {
                return;
            }

            popupRoot.SetActive(false);
        }

        private static TMP_Text ResolveText(GameObject root, string childName)
        {
            Transform child = ResolveChild(root, childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Button ResolveButton(GameObject root, string childName)
        {
            Transform child = ResolveChild(root, childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform ResolveChild(GameObject root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            return TransformHierarchyUtility.FindChildRecursive(root.transform, childName);
        }
    }
}
