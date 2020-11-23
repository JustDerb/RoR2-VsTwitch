using System;

namespace VsTwitch
{
    class UpdateBitsEvent : EventArgs
    {
        public int Bits { get; set; }
    }

    class BitsManager
    {
        public int BitGoal { get; set; }
        public int Bits { get; private set; }

        public event EventHandler<UpdateBitsEvent> OnUpdateBits;

        public BitsManager() : this(0)
        {
        }
        
        public BitsManager(int initialBits)
        {
            Bits = initialBits;
            BitGoal = int.MaxValue;
        }

        public void AddBits(int bits)
        {
            if (bits <= 0)
            {
                return;
            }

            Bits += bits;
            OnUpdateBits?.Invoke(this, new UpdateBitsEvent { Bits = Bits });
        }

        public void ResetBits(bool subtractGoal)
        {
            if (subtractGoal)
            {
                Bits = Math.Max(0, Bits - BitGoal);
            }
            else
            {
                Bits = 0;
            }
            OnUpdateBits?.Invoke(this, new UpdateBitsEvent { Bits = Bits });
        }
    }
}
