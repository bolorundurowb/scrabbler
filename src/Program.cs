using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using scrabbler.Models;

namespace scrabbler;

public class Program
{
    private static string[] _words;

    public static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand()
        {
            new Argument<string>("tiles", description: "Your tiles."),
            new Option<string?>(new[]
            {
                "--source-file", "-S"
            }, description: "A file with the words to be searched. Defaults to the in-built list."),
            new Option<string?>(new[]
            {
                "--constraints", "-C"
            }, description: "What target tiles you may want to incorporate."),
        };

        rootCommand.Description =
            "A console application to help generate possible scrabble words given a list of tile values and constraints";
        rootCommand.SetHandler(
            (string tiles, string? source, string? constraints, IConsole console) => { Process(tiles, source, constraints, console); });

        return rootCommand.InvokeAsync(args);
    }

    private static void Process(string tiles, string? source, string? constraints, IConsole console)
    {
        console.Out.Write(tiles);
    }

    private static IEnumerable<string> GetWords(string? source)
    {
        var def = Enumerable.Empty<string>();

        if (source != null)
            if (File.Exists(source))
                return File.ReadAllLines(source);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetFile("words.txt");

        if (stream == null)
            return def;

        using var sr = new StreamReader(stream);
        var contents = sr.ReadToEnd();
        return contents.Split("\n");
    }

    private static List<string> FindMatches(IEnumerable<char> tiles,
        List<CharacterConstraint> constraints)
    {
        IEnumerable<string> matches = _words;
        
        // words shorter than the maximum constraints can be ignored
        var maximumWordLength = constraints.MaxBy(x => x.Position)?.Position;

        if (maximumWordLength.HasValue) 
            matches = matches.Where(x => x.Length >= maximumWordLength);

        foreach (var constraint in constraints)
            matches =
                matches.Where(x => x[constraint.Position - 1] == constraint.Character);

        var tileFrequencyMap = GenerateCharFreqMap(tiles.Union(constraints.Select(x => x.Character)));

        matches = matches.Where(x =>
        {
            var charFrequencyMap = GenerateCharFreqMap(x);
            var charsExistInTiles = charFrequencyMap.Keys.All(y => tileFrequencyMap.ContainsKey(y));

            if (!charsExistInTiles)
                return false;

            return charFrequencyMap.Keys.All(y => charFrequencyMap[y] <= tileFrequencyMap[y]);
        });

        return matches.ToList();
    }

    private static Dictionary<char, int> GenerateCharFreqMap(IEnumerable<char> input) =>
        input.GroupBy(x => x)
            .ToDictionary(x => x.Key, y => y.Count());
}