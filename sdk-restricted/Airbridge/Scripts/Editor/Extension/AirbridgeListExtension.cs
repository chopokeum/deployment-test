#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;

internal static class AirbridgeListExtension
{
    internal static List<string> MergeDistinctTrimmed(this List<string> self, string str, char separator)
    {
        if (string.IsNullOrEmpty(str)) return self;
        self.AddRange(
            str.Split(separator)
                .Where(elem => !string.IsNullOrEmpty(elem))
                .Select(elem => elem.Trim())
                .Distinct()
                .ToList()
        );

        return self;
    }

    internal static List<string> MergeDistinctTrimmed(this List<string> self, List<string> list)
    {
        if (list == null || list.Count == 0) return self;
        self.AddRange(
            list.Where(elem => !string.IsNullOrEmpty(elem))
                .Select(elem => elem.Trim())
                .Distinct()
                .ToList()
        );

        return self;
    }

    internal static List<string> RemoveDuplicates(this List<string> self) => self.Distinct().ToList();
}

#endif