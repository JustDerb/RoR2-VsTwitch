using System.Collections.Generic;
using UnityEngine;

namespace VsTwitch
{
    class PercentileVoteStrategy<T> : IVoteStrategy<T>
    {
        public T GetWinner(Dictionary<string, T> votes)
        {
            Dictionary<T, int> totalVotes = new Dictionary<T, int>();

            foreach (var item in votes)
            {
                int total = totalVotes.TryGetValue(item.Value, out int tmp) ? tmp : 0;
                totalVotes[item.Value] = total++;
            }

            int maxWeight = votes.Count;
            int randomWeight = Random.Range(0, maxWeight);
            T winner = default;
            foreach (var item in totalVotes)
            {
                if (randomWeight <= item.Value)
                {
                    winner = item.Key;
                    break;
                }

                randomWeight -= item.Value;
            }

            return winner;
        }
    }
}
