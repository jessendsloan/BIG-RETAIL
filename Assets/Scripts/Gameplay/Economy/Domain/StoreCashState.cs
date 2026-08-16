using System;

namespace BigRetail.Economy.Domain
{
    /// <summary>
    /// Owns the store's authoritative liquid cash balance.
    ///
    /// Currency is stored as whole cents so gameplay never depends on
    /// floating-point rounding. Revenue and expenses may change the balance,
    /// but no operation can make it negative.
    /// </summary>
    public sealed class StoreCashState
    {
        public long BalanceCents { get; private set; }


        public StoreCashState(long openingBalanceCents)
        {
            if (openingBalanceCents < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openingBalanceCents),
                    openingBalanceCents,
                    "Opening cash cannot be negative.");
            }

            BalanceCents = openingBalanceCents;
        }


        public event Action BalanceChanged;


        public bool CanAfford(long amountCents)
        {
            return amountCents > 0
                && BalanceCents >= amountCents;
        }

        public bool TrySpend(long amountCents)
        {
            if (!CanAfford(amountCents))
            {
                return false;
            }

            BalanceCents -= amountCents;
            BalanceChanged?.Invoke();
            return true;
        }

        public void Credit(long amountCents)
        {
            if (amountCents <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amountCents),
                    amountCents,
                    "A cash credit must be greater than zero.");
            }

            if (BalanceCents > long.MaxValue - amountCents)
            {
                throw new OverflowException(
                    "The cash credit exceeds the supported balance.");
            }

            BalanceCents += amountCents;
            BalanceChanged?.Invoke();
        }
    }
}
