using System;
using System.Collections.Generic;

namespace SpriteLibraryTools
{
    // Digit-aware comparer so "Idle_2" sorts before "Idle_10".
    public sealed class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

        public int Compare(string a, string b)
        {
            if (a == null || b == null)
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);

            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    int startI = i, startJ = j;
                    while (i < a.Length && char.IsDigit(a[i])) i++;
                    while (j < b.Length && char.IsDigit(b[j])) j++;

                    string numA = a.Substring(startI, i - startI).TrimStart('0');
                    string numB = b.Substring(startJ, j - startJ).TrimStart('0');

                    if (numA.Length != numB.Length)
                        return numA.Length.CompareTo(numB.Length);

                    int numCompare = string.CompareOrdinal(numA, numB);
                    if (numCompare != 0)
                        return numCompare;
                }
                else
                {
                    int charCompare = char.ToLowerInvariant(a[i]).CompareTo(char.ToLowerInvariant(b[j]));
                    if (charCompare != 0)
                        return charCompare;
                    i++;
                    j++;
                }
            }

            return (a.Length - i).CompareTo(b.Length - j);
        }
    }
}
