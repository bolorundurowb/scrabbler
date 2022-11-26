using System.CommandLine;
using System.CommandLine.IO;
using System.Reflection;
using scrabbler.Models;

namespace scrabbler;

public class Program
{
    public static Task<int> Main(string[] args)
    {
        var sourceFileOption = new Option<string?>(new[] { "--source-file", "-S" },
            description: "A file with the words to be searched. Defaults to the in-built list.");

        var rootCommand = new RootCommand();
        rootCommand.AddGlobalOption(sourceFileOption);
        rootCommand.Description =
            "A console application to help with scrabble related computing.";

        var tilesArgument = new Argument<string>("tiles", description: "Your comma-separated tiles.");
        var constraintsOption = new Option<string?>(new[] { "--constraints", "-C" },
            description: "What target tiles you may want to incorporate.");

        var suggestCommand = new Command("suggest", "Suggest possible word combinations.");
        suggestCommand.AddArgument(tilesArgument);
        suggestCommand.AddOption(constraintsOption);
        rootCommand.AddCommand(suggestCommand);

        suggestCommand.SetHandler((string tiles, string? source, string? constraints) =>
            Process(tiles, source, constraints), tilesArgument, sourceFileOption, constraintsOption);

        return rootCommand.InvokeAsync(args);
    }

    private static void Process(string tiles, string? source, string? constraints)
    {
        var words = GetWords(source).ToList();

        if (!words.Any())
        {
            Console.WriteLine("No word list loaded. Cannot continue with matching.");
            return;
        }

        var parsedConstraints = ParseConstraints(constraints);
        var matches = FindMatches(words, tiles, parsedConstraints);

        Console.WriteLine($"{matches.Count} match(es) found:");

        foreach (var match in matches)
            Console.WriteLine(match);
    }

    private static IEnumerable<string> GetWords(string? source)
    {
        var def = Enumerable.Empty<string>();

        if (source != null)
            if (File.Exists(source))
                return File.ReadAllLines(source);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("scrabbler.words.txt");

        if (stream == null)
            return def;

        using var sr = new StreamReader(stream);
        var contents = sr.ReadToEnd();
        return contents.Split("\n");
    }

    private static List<CharacterConstraint> ParseConstraints(string? constraints)
    {
        var parsedConstraints = new List<CharacterConstraint>();

        if (string.IsNullOrWhiteSpace(constraints))
            return parsedConstraints;

        var parts = constraints.Split(',');

        // the constraints need to be in pairs
        if (parts.Length % 2 != 0)
            throw new ArgumentException("Invalid constraints. Constraints need to be in pairs.");

        for (var i = 0; i < parts.Length; i += 2)
        {
            var character = parts[i].First();
            var position = int.Parse(parts[i + 1]);
            parsedConstraints.Add(new CharacterConstraint(character, position));
        }

        return parsedConstraints;
    }

    private static List<string> FindMatches(List<string> words, IEnumerable<char> tiles,
        List<CharacterConstraint> constraints)
    {
        IEnumerable<string> matches = words;

        // words shorter than the maximum constraints can be ignored
        var maximumWordLength = constraints.MaxBy(x => x.Position)?.Position;

        if (maximumWordLength.HasValue)
            matches = matches.Where(x => x.Length >= maximumWordLength);

        foreach (var constraint in constraints)
            matches = matches.Where(x => x[constraint.Position - 1] == constraint.Character);

        var tileFrequencyMap = GenerateCharFreqMap(tiles.Union(constraints.Select(x => x.Character)));

        matches = matches.Where(x =>
        {
            var charFrequencyMap = GenerateCharFreqMap(x);
            var charsExistInTiles = charFrequencyMap.Keys.All(y => tileFrequencyMap.ContainsKey(y));
            return charsExistInTiles && charFrequencyMap.Keys.All(y => charFrequencyMap[y] <= tileFrequencyMap[y]);
        });

        return matches.ToList();
    }

    private static Dictionary<char, int> GenerateCharFreqMap(IEnumerable<char> input) =>
        input.GroupBy(x => x)
            .ToDictionary(x => x.Key, y => y.Count());
}
