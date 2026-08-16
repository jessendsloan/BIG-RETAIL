using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Describes the retail interactions supported from one side of a
    /// fixture. Multiple interactions may share the same side.
    /// </summary>
    [Flags]
    public enum FixtureAccessMode
    {
        None = 0,
        CustomerBrowse = 1 << 0,
        EmployeeStock = 1 << 1,
        CustomerCheckout = 1 << 2,
        EmployeeCheckout = 1 << 3
    }


    public static class FixtureAccessModeExtensions
    {
        private const FixtureAccessMode AllSupportedModes =
            FixtureAccessMode.CustomerBrowse
            | FixtureAccessMode.EmployeeStock
            | FixtureAccessMode.CustomerCheckout
            | FixtureAccessMode.EmployeeCheckout;


        public static bool IsSupported(
            this FixtureAccessMode mode)
        {
            return (mode & ~AllSupportedModes) == 0;
        }


        public static bool Includes(
            this FixtureAccessMode mode,
            FixtureAccessMode requestedMode)
        {
            return requestedMode != FixtureAccessMode.None
                && requestedMode.IsSupported()
                && (mode & requestedMode) == requestedMode;
        }
    }
}
