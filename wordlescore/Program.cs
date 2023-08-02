using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace worklescore;


internal record Entry(DateOnly Date, string Name, byte Score, int Game);


internal static class Extention
{
    internal static IEnumerable<string> ReadLines(this TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
            yield return line;

        yield break;
    }
}


public static class Program
{
    private const int ARGINDEX_CONTEXTDATE = 0;
    private const int ARGINDEX_RECORDCOUNT = 1;
    private const int ARGINDEX_FILEPATH = 2;
    private const int ARGINDEX_TOTAL = 3;


    private static IEnumerable<Entry> LoadData(DateOnly startDate, string path)
    {
        var data = new LinkedList<Entry>();
        var endDate = startDate.AddMonths(1).AddDays(-1);

        try
        {
            using var fs = new FileStream(path, FileMode.Open);
            using var sr = new StreamReader(fs);

            foreach (var line in sr.ReadLines())
            {
                var result = Regex.Match(line ?? string.Empty, "^(.{10}), \\d\\d:\\d\\d - ([\\w\\s]+?): Wordle (\\d+?) (\\d+?)/6.?$", RegexOptions.CultureInvariant);

                if (result is Match match && match.Success == true)
                {
                    var date = DateOnly.Parse($"{match.Groups[1].Value[6..]}-{match.Groups[1].Value[3..5]}-{match.Groups[1].Value[0..2]}");

                    if (date >= startDate && date <= endDate)
                        data.AddLast(new LinkedListNode<Entry>(new Entry(
                            date,
                            match.Groups[2].Value.Trim(),
                            Convert.ToByte(match.Groups[4].Value, CultureInfo.InvariantCulture),
                            Convert.ToInt32(match.Groups[3].Value, CultureInfo.InvariantCulture))));
                }
            }
            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            throw;
        }
    }


    internal static void CountRecords(IEnumerable<Entry> records, byte recordCount)
    {
        var data = records.ToLookup(x => x.Name);
        var results = new Dictionary<string, int>();

        foreach (var entry in data)
        {
            if (entry.Count() < 20)
                continue;

            var score = entry.OrderBy(x => x.Score).Take(recordCount).Sum(x => x.Score);
            results.Add(entry.Key, score);
        }

        Console.WriteLine(JsonSerializer.Serialize(results));
    }


    public static void Main(string[] args)
    {
        if (args is null || args.Length != ARGINDEX_TOTAL || !File.Exists(args[ARGINDEX_FILEPATH]) || !DateOnly.TryParse(args[ARGINDEX_CONTEXTDATE], out var targetDate) || !byte.TryParse(args[ARGINDEX_RECORDCOUNT], out var recordCount))
        {
            Console.WriteLine($"Please specify target date and input file.\r\nReceived: {JsonSerializer.Serialize(args)}");
            return;
        }

        var data = LoadData(targetDate.AddDays(-targetDate.Day + 1), args[ARGINDEX_FILEPATH]);
        Console.WriteLine($"Record Count: {data.Count()}");
        
        CountRecords(data, recordCount);
    }
}