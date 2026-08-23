using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    public enum StoreDefinitionKind
    {
        FloorFinish = 0,
        WallFinish = 1,
        Opening = 2,
        Fixture = 3,
        Department = 4,
        Product = 5,
        Supplier = 6
    }


    /// <summary>
    /// Narrow definition lookup used by preflight validation. Unity catalog
    /// adapters can implement this without making the authored-data assembly
    /// depend on every gameplay catalog.
    /// </summary>
    public interface IStoreDefinitionCatalog
    {
        bool Contains(
            StoreDefinitionKind kind,
            string definitionId);
    }


    /// <summary>
    /// Small engine-free lookup useful for tests, importers, and composition
    /// roots that have already resolved their permanent catalogs.
    /// </summary>
    public sealed class StoreDefinitionCatalog :
        IStoreDefinitionCatalog
    {
        private readonly Dictionary<
            StoreDefinitionKind,
            HashSet<string>> definitions =
                new Dictionary<
                    StoreDefinitionKind,
                    HashSet<string>>();


        public StoreDefinitionCatalog Add(
            StoreDefinitionKind kind,
            string definitionId)
        {
            string normalizedId =
                StoreDataIdentity.NormalizeRequired(
                    definitionId,
                    nameof(definitionId));

            if (!definitions.TryGetValue(
                    kind,
                    out HashSet<string> identifiers))
            {
                identifiers =
                    new HashSet<string>(
                        StringComparer.Ordinal);

                definitions.Add(kind, identifiers);
            }

            identifiers.Add(normalizedId);
            return this;
        }

        public bool Contains(
            StoreDefinitionKind kind,
            string definitionId)
        {
            return StoreDataIdentity.TryNormalize(
                       definitionId,
                       out string normalizedId)
                && definitions.TryGetValue(
                    kind,
                    out HashSet<string> identifiers)
                && identifiers.Contains(normalizedId);
        }
    }


    internal static class StoreDataIdentity
    {
        public static string NormalizeRequired(
            string value,
            string parameterName)
        {
            if (!TryNormalize(value, out string normalized))
            {
                throw new ArgumentException(
                    "An authored identifier cannot be empty.",
                    parameterName);
            }

            return normalized;
        }

        public static bool TryNormalize(
            string value,
            out string normalized)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                normalized = string.Empty;
                return false;
            }

            normalized =
                value.Trim().ToUpperInvariant();

            return true;
        }

        public static bool Equals(
            string left,
            string right)
        {
            return TryNormalize(left, out string normalizedLeft)
                && TryNormalize(right, out string normalizedRight)
                && string.Equals(
                    normalizedLeft,
                    normalizedRight,
                    StringComparison.Ordinal);
        }
    }
}
