using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Requires a specific placed, two-sided checkout fixture before settling
    /// a shopper-owned basket. Customer movement, queues, and staffing can
    /// extend this station check without changing inventory or accounting
    /// ownership.
    /// </summary>
    public sealed class FixtureCheckoutService : IDisposable
    {
        private readonly FixtureState fixtureState;
        private readonly FixtureSalesService sales;
        private bool isDisposed;


        public int OperationalCheckoutCount { get; private set; }

        public bool HasOperationalCheckout =>
            OperationalCheckoutCount > 0;


        public FixtureCheckoutService(
            FixtureState fixtureState,
            FixtureSalesService sales)
        {
            this.fixtureState =
                fixtureState
                ?? throw new ArgumentNullException(nameof(fixtureState));

            this.sales =
                sales
                ?? throw new ArgumentNullException(nameof(sales));

            fixtureState.FixtureAdded += HandleFixtureChanged;
            fixtureState.FixtureRemoved += HandleFixtureChanged;
            RefreshOperationalCheckoutCount();
        }


        public event Action AvailabilityChanged;


        public bool IsOperationalCheckout(
            FixtureInstanceId checkoutFixtureId)
        {
            return fixtureState.TryGetFixture(
                    checkoutFixtureId,
                    out FixtureInstance fixture)
                && HasCheckoutAccess(fixture);
        }

        public FixtureSaleResult TryProcessBasket(
            FixtureInstanceId checkoutFixtureId,
            ShoppingBasket basket)
        {
            if (!IsOperationalCheckout(checkoutFixtureId))
            {
                return FixtureSaleResult.Failed(
                    FixtureSaleOutcome.CheckoutUnavailable);
            }

            return sales.TryCompleteBasketSale(basket);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            fixtureState.FixtureAdded -= HandleFixtureChanged;
            fixtureState.FixtureRemoved -= HandleFixtureChanged;
            isDisposed = true;
        }


        private void HandleFixtureChanged(
            FixtureInstance fixture)
        {
            RefreshOperationalCheckoutCount();
        }

        private void RefreshOperationalCheckoutCount()
        {
            int previousCount = OperationalCheckoutCount;
            int nextCount = 0;

            foreach (FixtureInstance fixture
                     in fixtureState.EnumerateFixtures())
            {
                if (HasCheckoutAccess(fixture))
                {
                    nextCount++;
                }
            }

            OperationalCheckoutCount = nextCount;

            if (previousCount != nextCount)
            {
                AvailabilityChanged?.Invoke();
            }
        }

        private static bool HasCheckoutAccess(
            FixtureInstance fixture)
        {
            bool hasCustomerPosition = false;
            bool hasEmployeePosition = false;

            for (int index = 0;
                 index < fixture.ReservedAccessPoints.Count;
                 index++)
            {
                FixtureAccessMode mode =
                    fixture.ReservedAccessPoints[index].Mode;

                hasCustomerPosition |=
                    mode.Includes(FixtureAccessMode.CustomerCheckout);
                hasEmployeePosition |=
                    mode.Includes(FixtureAccessMode.EmployeeCheckout);
            }

            return hasCustomerPosition && hasEmployeePosition;
        }
    }
}
