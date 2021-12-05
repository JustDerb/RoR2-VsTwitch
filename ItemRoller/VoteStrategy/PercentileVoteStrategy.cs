using System.Collections.Generic;
using UnityEngine;

namespace VsTwitch
{
    public class PercentileVoteStrategy<T> : IVoteStrategy<T>
    {
        public T GetWinner(Dictionary<string, T> votes, T[] allChoices)
        {
            if (votes.Count == 0)
            {
                return allChoices[Random.Range(0, allChoices.Length)];
            }

            Dictionary<T, int> totalVotes = new Dictionary<T, int>();

            foreach (var item in votes)
            {
                int total = totalVotes.TryGetValue(item.Value, out int tmp) ? tmp : 0;
                totalVotes[item.Value] = ++total;
            }

            int maxWeight = votes.Count;
            int randomWeight = Random.Range(0, maxWeight);
            // Default to at least a random one
            T winner = allChoices[Random.Range(0, allChoices.Length)];
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
