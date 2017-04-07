using System;

namespace Spork.Extensions
{
    public static class StringExtensions
    {
        static readonly string[] AllowedLineSeparators =
        {
            Environment.NewLine,
            "\n\n"
        };

        public static string[] GetLines(this string str)
        {
            return str.Split(AllowedLineSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}