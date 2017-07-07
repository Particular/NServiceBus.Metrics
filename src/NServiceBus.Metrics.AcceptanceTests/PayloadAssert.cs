using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

public static class PayloadAssert
{
    public static void ContainsMeters(string payload, List<string> meters)
    {
        var matches = Regex.Matches(payload, @"""Name"":""(.+?(?=""))");
        var foundMeters = new List<string>();
        foreach (Match match in matches)
        {
            foundMeters.Add(match.Groups[1].Value);
        }

        CollectionAssert.AreEquivalent(foundMeters, meters);
    }
}