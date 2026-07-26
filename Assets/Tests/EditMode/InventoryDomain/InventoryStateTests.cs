using System;
using System.Collections.Generic;
using System.Linq;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Inventory.Domain.Tests
{
    public sealed class InventoryStateTests
    {
        [Test]
        public void InitialBalance_IsAvailableAtItsLocation()
        {
            ProductId productId =
                new ProductId("choco-bar");

            StorageLocationId backroomId =
                new StorageLocationId("backroom-a");

            InventoryState inventory =
                CreateInventory(
                    productId,
                    new[]
                    {
                        CreateLocation(
                            backroomId,
                            StorageRole.Backroom)
                    },
                    new[]
                    {
                        new StockBalance(
                            backroomId,
                            productId,
                            96)
                    });

            Assert.That(
                inventory.GetQuantity(
                    backroomId,
                    productId),
                Is.EqualTo(96));
        }

        [Test]
        public void KnownProductWithoutBalance_ReturnsZero()
        {
            ProductId productId =
                new ProductId("choco-bar");

            StorageLocationId salesFloorId =
                new StorageLocationId("sales-floor-a");

            InventoryState inventory =
                CreateInventory(
                    productId,
                    new[]
                    {
                        CreateLocation(
                            salesFloorId,
                            StorageRole.SalesFloor)
                    });

            Assert.That(
                inventory.GetQuantity(
                    salesFloorId,
                    productId),
                Is.Zero);
        }

        [Test]
        public void DuplicateLocation_Throws()
        {
            ProductId productId =
                new ProductId("choco-bar");

            StorageLocationId locationId =
                new StorageLocationId("backroom-a");

            Assert.Throws<ArgumentException>(
                () =>
                    CreateInventory(
                        productId,
                        new[]
                        {
                            CreateLocation(
                                locationId,
                                StorageRole.Backroom),
                            CreateLocation(
                                locationId,
                                StorageRole.SalesFloor)
                        }));
        }

        [Test]
        public void InitialBalanceForUnknownProduct_Throws()
        {
            ProductId knownProductId =
                new ProductId("choco-bar");

            ProductId unknownProductId =
                new ProductId("tomato");

            StorageLocationId locationId =
                new StorageLocationId("backroom-a");

            Assert.Throws<KeyNotFoundException>(
                () =>
                    CreateInventory(
                        knownProductId,
                        new[]
                        {
                            CreateLocation(
                                locationId,
                                StorageRole.Backroom)
                        },
                        new[]
                        {
                            new StockBalance(
                                locationId,
                                unknownProductId,
                                10)
                        }));
        }

        [Test]
        public void InitialBalanceForUnknownLocation_Throws()
        {
            ProductId productId =
                new ProductId("choco-bar");

            StorageLocationId knownLocationId =
                new StorageLocationId("backroom-a");

            StorageLocationId unknownLocationId =
                new StorageLocationId("missing");

            Assert.Throws<KeyNotFoundException>(
                () =>
                    CreateInventory(
                        productId,
                        new[]
                        {
                            CreateLocation(
                                knownLocationId,
                                StorageRole.Backroom)
                        },
                        new[]
                        {
                            new StockBalance(
                                unknownLocationId,
                                productId,
                                10)
                        }));
        }

        [Test]
        public void DuplicateInitialBalance_Throws()
        {
            ProductId productId =
                new ProductId("choco-bar");

            StorageLocationId locationId =
                new StorageLocationId("backroom-a");

            Assert.Throws<ArgumentException>(
                () =>
                    CreateInventory(
                        productId,
                        new[]
                        {
                            CreateLocation(
                                locationId,
                                StorageRole.Backroom)
                        },
                        new[]
                        {
                            new StockBalance(
                                locationId,
                                productId,
                                10),
                            new StockBalance(
                                locationId,
                                productId,
                                5)
                        }));
        }

        [Test]
        public void EnumerateBalances_OmitsZeroBalances()
        {
            ProductId productId =
                new ProductId("choco-bar");

            StorageLocationId locationId =
                new StorageLocationId("backroom-a");

            InventoryState inventory =
                CreateInventory(
                    productId,
                    new[]
                    {
                        CreateLocation(
                            locationId,
                            StorageRole.Backroom)
                    },
                    new[]
                    {
                        new StockBalance(
                            locationId,
                            productId,
                            0)
                    });

            Assert.That(
                inventory.EnumerateBalances().Count(),
                Is.Zero);
        }


        private static InventoryState CreateInventory(
            ProductId productId,
            IEnumerable<StorageLocationDefinition> locations,
            IEnumerable<StockBalance> initialBalances = null)
        {
            ProductCatalog catalog =
                new ProductCatalog(
                    new[]
                    {
                        new ProductDefinition(
                            productId,
                            "Chocolate Bar",
                            new ProductCategoryId("candy"),
                            StockUnit.Each)
                    });

            return new InventoryState(
                catalog,
                locations,
                initialBalances);
        }

        private static StorageLocationDefinition CreateLocation(
            StorageLocationId locationId,
            StorageRole role)
        {
            return new StorageLocationDefinition(
                locationId,
                locationId.Value,
                role);
        }
    }
}
