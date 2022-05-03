using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;

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
            (string tiles, string? source, string? constraints) => { Process(tiles, source, constraints); });

        return rootCommand.InvokeAsync(args);
    }

    private static void Process(string tiles, string? source, string? constraints)
    {
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
}