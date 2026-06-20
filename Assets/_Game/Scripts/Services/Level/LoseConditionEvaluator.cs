namespace FlowBlast.Services.Level
{
    public sealed class LoseConditionEvaluator
    {
        private const string TimeoutLoseReason = "Don't give up! Try again!";

        private readonly float initialTimeLimitSeconds;
        private readonly float reviveBonusTimeSeconds;
        private readonly int reviveCountdownSeconds;

        private float remainingTimeSeconds;
        private float reviveCountdownRemainingSeconds;
        private bool canRevive;
        private bool isReviveOfferPending;

        public LoseConditionEvaluator(
            float initialTimeLimitSeconds,
            float reviveBonusTimeSeconds,
            int reviveCountdownSeconds)
        {
            this.initialTimeLimitSeconds = initialTimeLimitSeconds;
            this.reviveBonusTimeSeconds = reviveBonusTimeSeconds;
            this.reviveCountdownSeconds = reviveCountdownSeconds;
        }

        public float RemainingTimeSeconds => remainingTimeSeconds;
        public float ReviveCountdownRemainingSeconds => reviveCountdownRemainingSeconds;
        public bool CanRevive => canRevive;
        public int ReviveCountdownSeconds => reviveCountdownSeconds;
        public bool IsReviveOfferPending => isReviveOfferPending;

        public void StartLevel()
        {
            remainingTimeSeconds = initialTimeLimitSeconds;
            reviveCountdownRemainingSeconds = 0f;
            canRevive = true;
            isReviveOfferPending = false;
        }

        public void Tick(float deltaTime)
        {
            if (isReviveOfferPending)
            {
                TickReviveCountdown(deltaTime);
                return;
            }

            if (remainingTimeSeconds <= 0f)
            {
                return;
            }

            remainingTimeSeconds -= deltaTime;

            if (remainingTimeSeconds < 0f)
            {
                remainingTimeSeconds = 0f;
            }
        }

        public bool IsLose(out string reason)
        {
            reason = string.Empty;

            if (remainingTimeSeconds > 0f)
            {
                return false;
            }

            reason = TimeoutLoseReason;
            return true;
        }

        public bool TryConsumeReviveOffer(out int countdownSeconds)
        {
            countdownSeconds = 0;

            if (!canRevive || isReviveOfferPending)
            {
                return false;
            }

            isReviveOfferPending = true;
            reviveCountdownRemainingSeconds = reviveCountdownSeconds;
            countdownSeconds = reviveCountdownSeconds;
            return true;
        }

        public bool TryRevive()
        {
            if (!isReviveOfferPending || !canRevive)
            {
                return false;
            }

            canRevive = false;
            isReviveOfferPending = false;
            reviveCountdownRemainingSeconds = 0f;
            remainingTimeSeconds = reviveBonusTimeSeconds;
            return true;
        }

        public void DeclineRevive()
        {
            isReviveOfferPending = false;
            reviveCountdownRemainingSeconds = 0f;
            canRevive = false;
        }

        public bool HasReviveCountdownExpired()
        {
            return isReviveOfferPending && reviveCountdownRemainingSeconds <= 0f;
        }

        private void TickReviveCountdown(float deltaTime)
        {
            if (reviveCountdownRemainingSeconds <= 0f)
            {
                return;
            }

            reviveCountdownRemainingSeconds -= deltaTime;

            if (reviveCountdownRemainingSeconds < 0f)
            {
                reviveCountdownRemainingSeconds = 0f;
            }
        }
    }
}
