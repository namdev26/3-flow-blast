using System.Collections.Generic;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltFollowerRegistry
    {
        private readonly List<IBeltFollower> followers = new List<IBeltFollower>();

        public void Register(IBeltFollower follower)
        {
            if (!followers.Contains(follower))
            {
                followers.Add(follower);
            }
        }

        public void Unregister(IBeltFollower follower)
        {
            followers.Remove(follower);
        }

        public IReadOnlyList<IBeltFollower> GetFollowers()
        {
            return followers;
        }
    }
}
