using System;
using NUnit.Framework;

namespace BigRetail.Economy.Domain.Tests
{
    public sealed class StoreCashStateTests
    {
        [Test]
        public void TrySpend_AffordableExpense_DeductsExactCents()
        {
            StoreCashState cash = new StoreCashState(250000);

            bool succeeded = cash.TrySpend(3600);

            Assert.That(succeeded, Is.True);
            Assert.That(cash.BalanceCents, Is.EqualTo(246400));
        }

        [Test]
        public void TrySpend_ExpenseExceedsBalance_LeavesCashUnchanged()
        {
            StoreCashState cash = new StoreCashState(3000);

            bool succeeded = cash.TrySpend(3600);

            Assert.That(succeeded, Is.False);
            Assert.That(cash.BalanceCents, Is.EqualTo(3000));
        }

        [Test]
        public void Credit_PositiveRevenue_AddsExactCents()
        {
            StoreCashState cash = new StoreCashState(3000);

            cash.Credit(1250);

            Assert.That(cash.BalanceCents, Is.EqualTo(4250));
        }

        [Test]
        public void Constructor_NegativeOpeningBalance_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new StoreCashState(-1));
        }
    }
}
