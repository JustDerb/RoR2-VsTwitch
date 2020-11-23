using System.Collections.Generic;

namespace VsTwitch
{
    interface IVoteStrategy<T>
    {
        T GetWinner(Dictionary<string, T> votes);
    }
}
