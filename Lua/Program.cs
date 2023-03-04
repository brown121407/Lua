using CommandLine;

namespace Lua;

public static class Program
{
    private class Options
    {
        [Value(0, Default = null)]
        public string? File { get; set; }
    }

    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                options =>
                {
                    if (options.File is not null)
                    {
                        RunFile(options.File);
                    }
                    else
                    {
                        Repl();
                    }

                    return 0;
                },
                _ => 1);
    }

    private static void RunFile(string path)
    {
        var fileContents = File.ReadAllText(path);
        var lexer = new Lexer(fileContents);
        var tokens = lexer.Lex();
        foreach (var token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    private static void Repl()
    {
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var lexer = new Lexer(line);

            var tokens = lexer.Lex(skipErrors: true);

            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }
        }
    }
}