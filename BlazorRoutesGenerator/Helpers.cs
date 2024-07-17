using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorRoutesGenerator;

internal static class Helpers
{
    public static IEnumerable<string> FindMinimalSegments(IEnumerable<string> input)
    {
        List<string[]> splitStrings = input.Select(s => s.Split('.')).ToList();

        int inputCount = input.Count();
        for (int i = 0; i < inputCount; i++)
        {
            int minLength = 1;
            string uniqueSegment = string.Empty;

            while (minLength <= splitStrings[i].Length)
            {
                string[] segment = splitStrings[i].Skip(splitStrings[i].Length - minLength).ToArray();
                uniqueSegment = string.Join(".", segment);

                if (IsUnique(uniqueSegment, splitStrings))
                {
                    break;
                }
                minLength++;
            }

            yield return uniqueSegment;
        }
    }

    private static bool IsUnique(string segment, List<string[]> splitStrings)
    {
        int count = 0;

        foreach (string[] parts in splitStrings)
        {
            string currentSegment = string.Join(".", parts.Skip(parts.Length - segment.Split('.').Length));
            if (currentSegment == segment)
            {
                count++;
            }
        }

        return count == 1;
    }
}
