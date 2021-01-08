using RoR2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace VsTwitch
{

    class ItemRollerManager : IItemRoller<PickupIndex>
    {
        private readonly ReaderWriterLockSlim cacheLock;
        private readonly IVoteStrategy<PickupIndex> voteStrategy;

        private readonly BlockingCollection<Vote> voteQueue;
        private Vote currentVote;
        private Vote previousVote;
        private int id;

        public event EventHandler<IDictionary<int, PickupIndex>> OnVoteStart;

        public ItemRollerManager(IVoteStrategy<PickupIndex> voteStrategy)
        {
            this.cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            this.voteQueue = new BlockingCollection<Vote>();
            this.voteStrategy = voteStrategy;
            this.id = 1;
        }

        public int QueueSize()
        {
            cacheLock.EnterReadLock();
            try
            {
                return voteQueue.Count;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public void EndVote()
        {
            OnVoteEnd();
        }

        public void ClearVotes()
        {
            cacheLock.EnterWriteLock();
            try
            {
                while (voteQueue.TryTake(out _)) { }
                EndVote();
                previousVote = null;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        private void OnVoteEnd()
        {
            try
            {
                cacheLock.EnterWriteLock();
                try
                {
                    if (currentVote != null)
                    {
                        currentVote.EndVote();
                        previousVote = currentVote;
                        currentVote = null;
                    }
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }

                TryStartVote();
            } catch (Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

        public void RollForItem(List<PickupIndex> indices, Action<PickupIndex> callback)
        {
            Vote vote = new Vote(indices, voteStrategy, Interlocked.Increment(ref id));
            vote.OnVoteStart += OnVoteStart;
            vote.OnVoteEnd += (sender, e) =>
            {
                callback?.Invoke(e);
            };

            cacheLock.EnterWriteLock();
            try
            {
                if (isSameVote(vote, currentVote))
                {
                    System.Console.WriteLine($"WARNING: Not adding roll for {string.Join(", ", vote.GetCandidates().Values)} with id {vote.GetId()} " +
                        $"because the previous vote was exactly that. There might be another mod causing issues making rolling busted.");
                    return;
                }

                System.Console.WriteLine($"Adding roll for {string.Join(", ", vote.GetCandidates().Values)} with id {vote.GetId()}");
                voteQueue.Add(vote);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

            TryStartVote();
        }

        private bool isSameVote(Vote vote, Vote lastVote)
        {
            if (vote == null || lastVote == null)
            {
                return false;
            }
            HashSet<PickupIndex> vote1 = new HashSet<PickupIndex>(vote.GetCandidates().Values);
            HashSet<PickupIndex> vote2 = new HashSet<PickupIndex>(lastVote.GetCandidates().Values);
            return vote1.SetEquals(vote2);
        }

        public void AddVote(string username, int index)
        {
            cacheLock.EnterReadLock();
            try
            {
                currentVote?.AddVote(username, index);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        private void TryStartVote()
        {
            cacheLock.EnterWriteLock();
            try
            {
                if (currentVote == null)
                {
                    while (true)
                    {
                        if (!voteQueue.TryTake(out currentVote))
                        {
                            return;
                        }

                        if (isSameVote(previousVote, currentVote))
                        {
                            System.Console.WriteLine($"WARNING: Not starting vote for {string.Join(", ", currentVote.GetCandidates().Values)} with id {currentVote.GetId()} " +
                                $"because the previous vote was exactly that. There might be another mod causing issues making rolling busted.");
                            continue;
                        }

                        currentVote.StartVote();
                        return;
                    }
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }
    }
}
