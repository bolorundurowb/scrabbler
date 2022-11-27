using System.CommandLine;
using System.Reflection;
using scrabbler.Models;

namespace scrabbler;

public class Program
{
    private static readonly Dictionary<char, int> CharacterPointMap = new()
    {
        { 'A', 1 }, { 'B', 3 }, { 'C', 3 }, { 'D', 2 }, { 'E', 1 },
        { 'F', 4 }, { 'G', 2 }, { 'H', 4 }, { 'I', 1 }, { 'J', 1 },
        { 'K', 5 }, { 'L', 1 }, { 'M', 3 }, { 'N', 1 }, { 'O', 1 },
        { 'P', 3 }, { 'Q', 10 }, { 'R', 1 }, { 'S', 1 }, { 'T', 1 },
        { 'U', 1 }, { 'V', 4 }, { 'W', 4 }, { 'X', 8 }, { 'Y', 4 },
        { 'Z', 10 }
    };

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
        var maxLengthOption = new Option<int?>(new[] { "--max-length", "-M" },
            description: "The maximum length suggestions should be.");

        var suggestCommand = new Command("suggest", "Suggest possible word combinations.");
        suggestCommand.AddArgument(tilesArgument);
        suggestCommand.AddOption(constraintsOption);
        suggestCommand.AddOption(maxLengthOption);
        rootCommand.AddCommand(suggestCommand);

        suggestCommand.SetHandler((string tiles, string? source, string? constraints, int? maxLength) =>
                Process(tiles, source, constraints, maxLength), tilesArgument, sourceFileOption, constraintsOption,
            maxLengthOption);

        return rootCommand.InvokeAsync(args);
    }

    private static void Process(string tiles, string? source, string? constraints, int? maxLength)
    {
        var words = GetWords(source).ToList();

        if (!words.Any())
        {
            Console.WriteLine("No word list loaded. Cannot continue with matching.");
            return;
        }

        var parsedConstraints = ParseConstraints(constraints);
        var matches = FindMatches(words, tiles, parsedConstraints, maxLength);

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

    private static List<string> FindMatches(List<string> words, string tiles, List<CharacterConstraint> constraints,
        int? maxLength)
    {
        IEnumerable<string> matches = words;

        // maximum length that could possibly match
        var length = maxLength ?? (tiles.Length + constraints.Count);
        matches = matches.Where(x => x.Length <= length);

        // limit to words that match constraints
        matches = matches.Where(x =>
        {
            var constraintsThatApply = constraints.Where(y => y.Position < x.Length);

            foreach (var (character, position) in constraintsThatApply)
            {
                if (x[position - 1] == character)
                    continue;

                return false;
            }

            return true;
        });

        var tileFrequencyMap = GenerateCharFreqMap(
            tiles.Trim()
                .Replace(",", string.Empty)
                .Concat(constraints.Select(x => x.Character))
        );

        matches = matches.Where(x =>
        {
            var charFrequencyMap = GenerateCharFreqMap(x);
            var charsExistInTiles = charFrequencyMap.Keys.All(y => tileFrequencyMap.ContainsKey(y));
            var charFrequencyMatches = charFrequencyMap.Keys.All(y => charFrequencyMap[y] <= tileFrequencyMap[y]);
            return charsExistInTiles && charFrequencyMatches;
        });

        return matches.ToList();
    }

    private static Dictionary<char, int> GenerateCharFreqMap(IEnumerable<char> input) =>
        input.GroupBy(x => x)
            .ToDictionary(x => x.Key, y => y.Count());

    private static int PointsForWord(string word)
    {
        var sum = 0;
        word = word.ToUpperInvariant();

        foreach (var character in word)
            sum += CharacterPointMap[character];

        return sum;
    }
}
