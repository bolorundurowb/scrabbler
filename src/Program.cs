using System.CommandLine;

namespace scrabbler;

public class Program
{
    private const string SourceFileName = "./words.txt";
    private static readonly string[] _words;

    static Program() => _words = File.ReadLines(SourceFileName).ToArray();

    public static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand()
        {
            new Option<string>(new[]
            {
                "--source-file", "-S"
            }, () => SourceFileName, "A file with the words to be searched. Defaults to the in-built list."),
            new Option<string>(new[]
            {
                "--tiles", "-T"
            }, description: "Your tiles."),
            new Option<string>(new[]
            {
                "--constraints", "-C"
            }, description: "What target tiles you may want to incorporate."),
        };

        rootCommand.Description =
            "A console application to help generate possible scrabble words given a list of tile values and constraints";
        
        return  rootCommand.InvokeAsync(args);
    }
}