using System;
using VRage.Game;

namespace GlobalMarket
{
    internal static class EqualsExtensions
    {
        internal static bool EqualsIgnoreCase(this string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}