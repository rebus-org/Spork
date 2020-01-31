using System;

namespace Spork.Extensions
{
    public static class StringExtensions
    {
        static readonly string[] AllowedLineSeparators =
        {
            Environment.NewLine,
            "\n",
            "\r"
        };

        public static string[] GetLines(this string str) => str.Split(AllowedLineSeparators, StringSplitOptions.RemoveEmptyEntries);
    }
}