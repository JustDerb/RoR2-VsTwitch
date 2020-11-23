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

        public event EventHandler<IDictionary<int, PickupIndex>> OnVoteStart;

        public ItemRollerManager(IVoteStrategy<PickupIndex> voteStrategy)
        {
            this.cacheLock = new ReaderWriterLockSlim();
            this.voteQueue = new BlockingCollection<Vote>();
            this.voteStrategy = voteStrategy;
        }

        public void EndVote()
        {
            OnVoteEnd();
        }

        private void OnVoteEnd()
        {
            try
            {
                cacheLock.EnterWriteLock();
                try
                {
                    currentVote.EndVote();
                    currentVote = null;
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
            Vote vote = new Vote(indices, voteStrategy);
            vote.OnVoteStart += OnVoteStart;
            vote.OnVoteEnd += (sender, e) =>
            {
                callback?.Invoke(e);
            };

            cacheLock.EnterWriteLock();
            try
            {
                voteQueue.Add(vote);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

            TryStartVote();
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
                    if (!voteQueue.TryTake(out currentVote))
                    {
                        return;
                    }
                    currentVote.StartVote();
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }
    }
}
