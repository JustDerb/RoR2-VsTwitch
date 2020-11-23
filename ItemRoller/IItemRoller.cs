using System;
using System.Collections.Generic;

namespace VsTwitch
{
    interface IItemRoller<T>
    {
        void RollForItem(List<T> indices, Action<T> callback);
    }
}
