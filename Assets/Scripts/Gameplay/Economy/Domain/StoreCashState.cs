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

        public bool IsUnlimited { get; }


        public StoreCashState(long openingBalanceCents)
            : this(openingBalanceCents, false)
        {
        }


        private StoreCashState(
            long openingBalanceCents,
            bool isUnlimited)
        {
            if (openingBalanceCents < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openingBalanceCents),
                    openingBalanceCents,
                    "Opening cash cannot be negative.");
            }

            BalanceCents = openingBalanceCents;
            IsUnlimited = isUnlimited;
        }


        public event Action BalanceChanged;


        public static StoreCashState CreateUnlimited(
            long displayedBalanceCents)
        {
            return new StoreCashState(
                displayedBalanceCents,
                true);
        }


        public bool CanAfford(long amountCents)
        {
            return amountCents > 0
                && (IsUnlimited
                    || BalanceCents >= amountCents);
        }

        public bool TrySpend(long amountCents)
        {
            if (!CanAfford(amountCents))
            {
                return false;
            }

            if (IsUnlimited)
            {
                return true;
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

            if (IsUnlimited)
            {
                return;
            }

            if (BalanceCents > long.MaxValue - amountCents)
            {
                throw new OverflowException(
                    "The cash credit exceeds the supported balance.");
            }

            BalanceCents += amountCents;
            BalanceChanged?.Invoke();
        }

        /// <summary>
        /// Restores an authoritative balance from a validated scenario or
        /// save snapshot without replacing the state object observed by UI.
        /// </summary>
        public void RestoreBalance(long balanceCents)
        {
            if (balanceCents < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(balanceCents),
                    balanceCents,
                    "A restored cash balance cannot be negative.");
            }

            if (BalanceCents == balanceCents)
            {
                return;
            }

            BalanceCents = balanceCents;
            BalanceChanged?.Invoke();
        }
    }
}
