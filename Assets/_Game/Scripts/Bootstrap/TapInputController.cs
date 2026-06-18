using FlowBlast.Core.Events;
using FlowBlast.Core.Utilities;
using FlowBlast.Presentation.Box;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlowBlast.Bootstrap
{
    public sealed class TapInputController : MonoBehaviour
    {
        private GameplayContext context;
        private Camera gameplayCamera;

        public void Initialize(GameplayContext gameplayContext)
        {
            context = gameplayContext;
            gameplayCamera = Camera.main;
            context.EventBus.Subscribe<LevelWonEvent>(OnLevelWon);
            context.EventBus.Subscribe<LevelLostEvent>(OnLevelLost);
        }

        private void OnDestroy()
        {
            if (context == null)
            {
                return;
            }

            context.EventBus.Unsubscribe<LevelWonEvent>(OnLevelWon);
            context.EventBus.Unsubscribe<LevelLostEvent>(OnLevelLost);
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            if (!WasTapPressedThisFrame())
            {
                return;
            }

            if (TryHandleBoardBoxClick())
            {
                return;
            }

            context.LevelController.TrySendFrontBoxToBelt();
        }

        private bool TryHandleBoardBoxClick()
        {
            if (!BoxClickRaycastUtility.TryGetClickedBoxView(gameplayCamera, out BoxView boxView))
            {
                return false;
            }

            if (!boxView.CanReceiveBoardClick || boxView.Model == null)
            {
                return false;
            }

            return context.LevelController.TrySendBoardBoxToConveyor(boxView.Model);
        }

        private static bool WasTapPressedThisFrame()
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null
                && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void OnLevelWon(LevelWonEvent gameEvent)
        {
            Debug.Log($"[FlowBlast] Level won: {gameEvent.LevelId}");
        }

        private void OnLevelLost(LevelLostEvent gameEvent)
        {
            Debug.Log($"[FlowBlast] Level lost: {gameEvent.LevelId} | {gameEvent.Reason}");
        }
    }
}