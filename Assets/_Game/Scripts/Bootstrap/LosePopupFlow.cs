using System;
using FlowBlast.Core.Events;
using FlowBlast.Core.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowBlast.Bootstrap
{
    public sealed class LosePopupFlow : IDisposable
    {
        private const string MessageTextObjectName = "MessageText";
        private const string RetryButtonObjectName = "RetryButton";
        private const string HomeButtonObjectName = "HomeButton";

        private readonly IGameEventBus eventBus;
        private readonly Action onRetryRequested;
        private readonly Action onHomeRequested;
        private readonly GameObject popupRoot;
        private readonly TMP_Text messageText;
        private readonly Button retryButton;
        private readonly Button homeButton;

        public LosePopupFlow(
            IGameEventBus eventBus,
            GameObject popupRoot,
            Action onRetryRequested,
            Action onHomeRequested)
        {
            this.eventBus = eventBus;
            this.popupRoot = popupRoot;
            this.onRetryRequested = onRetryRequested;
            this.onHomeRequested = onHomeRequested;

            messageText = ResolveText(popupRoot, MessageTextObjectName);
            retryButton = ResolveButton(popupRoot, RetryButtonObjectName);
            homeButton = ResolveButton(popupRoot, HomeButtonObjectName);

            Hide();
            RegisterEvents();
        }

        public void Dispose()
        {
            eventBus?.Unsubscribe<LevelLostEvent>(OnLevelLost);

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryButtonClicked);
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            }
        }

        private void RegisterEvents()
        {
            eventBus?.Subscribe<LevelLostEvent>(OnLevelLost);

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryButtonClicked);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeButtonClicked);
            }
        }

        private void OnLevelLost(LevelLostEvent gameEvent)
        {
            if (gameEvent.CanRevive)
            {
                return;
            }

            UpdateMessage(gameEvent.Reason);
            Show();
        }

        private void OnRetryButtonClicked()
        {
            Hide();
            onRetryRequested?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            Hide();
            onHomeRequested?.Invoke();
        }

        private void UpdateMessage(string reason)
        {
            if (messageText == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }

            messageText.text = reason;
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
