using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

public static class PayloadAssert
{
    public static void ContainsMeters(string payload)
    {
        var matches = Regex.Matches(payload, @"""Name"":""(.+?(?=""))");
        var foundMeters = new List<string>();
        foreach (Match match in matches)
        {
            foundMeters.Add(match.Groups[1].Value);
        }

        CollectionAssert.AreEquivalent(foundMeters, meters);
    }

    static List<string> meters = new List<string>
    {
        "# of message failures / sec",
        "# of messages pulled from the input queue / sec",
        "# of messages successfully processed / sec",
        "Critical Time",
        "Processing Time"
    };
}