using UnityEngine;

namespace FlowBlast.Bootstrap
{
    public sealed class GameplayLoop : MonoBehaviour
    {
        private GameplayContext context;

        public void Initialize(GameplayContext gameplayContext)
        {
            context = gameplayContext;
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            context.LevelController.Tick(Time.deltaTime);
        }
    }
}
