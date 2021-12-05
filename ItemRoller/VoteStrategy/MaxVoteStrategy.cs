﻿using System.Collections.Generic;

namespace VsTwitch
{
    public class MaxVoteStrategy<T> : IVoteStrategy<T>
    {
        public T GetWinner(Dictionary<string, T> votes, T[] allChoices)
        {
            if (votes.Count == 0)
            {
                // First item wins
                return allChoices[0];
            }

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

            T winner = default;
            int highestVote = -1;
            foreach (var tally in totalVotes)
            {
                if (tally.Value > highestVote)
                {
                    winner = tally.Key;
                    highestVote = tally.Value;
                }
            }
            return winner;
        }
    }
}
