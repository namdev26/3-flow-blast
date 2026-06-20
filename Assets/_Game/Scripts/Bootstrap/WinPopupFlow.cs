using System;
using FlowBlast.Core.Events;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowBlast.Bootstrap
{
    public sealed class WinPopupFlow : IDisposable
    {
        private const string RewardTextObjectName = "RewardText";
        private const string NextButtonObjectName = "NextButton";
        private const string DoubleButtonObjectName = "DoubleButton";
        private const int DefaultRewardCoins = 100;

        private readonly IGameEventBus eventBus;
        private readonly Action onNextLevelRequested;
        private readonly GameObject popupRoot;
        private readonly TMP_Text rewardText;
        private readonly Button nextButton;
        private readonly Button doubleButton;
        private readonly int rewardCoins;

        public WinPopupFlow(
            IGameEventBus eventBus,
            GameObject popupRoot,
            Action onNextLevelRequested,
            int rewardCoins = DefaultRewardCoins)
        {
            this.eventBus = eventBus;
            this.popupRoot = popupRoot;
            this.onNextLevelRequested = onNextLevelRequested;
            this.rewardCoins = rewardCoins;

            rewardText = ResolveText(popupRoot, RewardTextObjectName);
            nextButton = ResolveButton(popupRoot, NextButtonObjectName);
            doubleButton = ResolveButton(popupRoot, DoubleButtonObjectName);

            Hide();
            RegisterEvents();
        }

        public void Dispose()
        {
            eventBus?.Unsubscribe<LevelWonEvent>(OnLevelWon);

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnNextButtonClicked);
            }

            if (doubleButton != null)
            {
                doubleButton.onClick.RemoveListener(OnDoubleButtonClicked);
            }
        }

        private void RegisterEvents()
        {
            eventBus?.Subscribe<LevelWonEvent>(OnLevelWon);

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }

            if (doubleButton != null)
            {
                doubleButton.onClick.AddListener(OnDoubleButtonClicked);
            }
        }

        private void OnLevelWon(LevelWonEvent gameEvent)
        {
            UpdateRewardText(rewardCoins);
            Show();
        }

        private void OnNextButtonClicked()
        {
            Hide();
            onNextLevelRequested?.Invoke();
        }

        private void OnDoubleButtonClicked()
        {
            UpdateRewardText(rewardCoins * 2);
        }

        private void UpdateRewardText(int coins)
        {
            if (rewardText == null)
            {
                return;
            }

            rewardText.text = $"+{coins} Coins";
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
