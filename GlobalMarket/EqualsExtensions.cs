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
        
        internal static bool EqualsIgnoreCase(this MyDefinitionId a, MyDefinitionId b)
        {
            return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}