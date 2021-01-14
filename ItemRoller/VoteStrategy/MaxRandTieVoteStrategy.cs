using System.Collections.Generic;
using UnityEngine;

namespace VsTwitch
{
    public class MaxRandTieVoteStrategy<T> : IVoteStrategy<T>
    {
        public T GetWinner(Dictionary<string, T> votes)
        {

            Dictionary<T, int> totalVotes = new Dictionary<T, int>();
            
            foreach (var item in votes)
            {
                if (totalVotes.TryGetValue(item.Value, out int total))
                {
                    totalVotes[item.Value] = total + 1;
                }
                else
                {
                    totalVotes[item.Value] = 1;
                }
            }

            List<T> winners = new List<T>();
            int highestVote = -1;
            foreach (var tally in totalVotes)
            {
                if (tally.Value > highestVote)
                {
                    winners.Clear();
                    winners.Add(tally.Key);
                    highestVote = tally.Value;
                }
                else if (tally.Value == highestVote)
                {
                    winners.Add(tally.Key);
                }
            }

            return winners[Random.Range(0, winners.Count)];
        }
    }
}
