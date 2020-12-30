using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VsTwitch
{
    class Vote
    {
        private readonly Dictionary<int, PickupIndex> indices;
        private readonly IVoteStrategy<PickupIndex> strategy;
        private readonly Dictionary<string, PickupIndex> votes;
        private readonly int id;

        public event EventHandler<IDictionary<int, PickupIndex>> OnVoteStart;
        public event EventHandler<PickupIndex> OnVoteEnd;

        public Vote(List<PickupIndex> indices, IVoteStrategy<PickupIndex> strategy, int id)
        {
            this.indices = new Dictionary<int, PickupIndex>();
            int i = 1;
            foreach (var item in indices)
            {
                this.indices[i] = item;
                i++;
            }
            this.strategy = strategy;
            votes = new Dictionary<string, PickupIndex>();
            this.id = id;
        }

        public IDictionary<int, PickupIndex> GetCandidates()
        {
            return new ReadOnlyDictionary<int, PickupIndex>(indices);
        }

        public int GetId()
        {
            return id;
        }

        public void StartVote()
        {
            OnVoteStart?.Invoke(this, GetCandidates());
        }

        public void AddVote(string username, int index)
        {
            if (!indices.TryGetValue(index, out PickupIndex pickupIndex))
            {
                return;
            }
            votes[username] = pickupIndex;
        }

        public void EndVote()
        {
            if (votes.Count == 0)
            {
                // It's quiet in here...
                if (indices.Count == 0)
                {
                    // Nothing to return!
                    OnVoteEnd?.Invoke(this, PickupIndex.none);
                    return;
                }
                var e = indices.GetEnumerator();
                e.MoveNext();
                OnVoteEnd?.Invoke(this, e.Current.Value);
            }
            else
            {
                OnVoteEnd?.Invoke(this, strategy.GetWinner(votes));
            }
        }

    }
}
