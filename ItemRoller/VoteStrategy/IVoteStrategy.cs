using System.Collections.Generic;

namespace VsTwitch
{
    public interface IVoteStrategy<T>
    {
        T GetWinner(Dictionary<string, T> votes, T[] allChoices);
    }
}
