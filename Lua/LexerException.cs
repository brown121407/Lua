namespace Lua;

public class LexerException : Exception
{
    public LexerException(string message, int line, int col) : base(FormatMessage(message, line, col))
    {

    }

    public LexerException(string message, Exception inner, int line, int col) : base(FormatMessage(message, line, col), inner)
    {

    }

    private static string FormatMessage(string message, int line, int col)
    {
        return $"At {line}:{col}: {message}";
    }
}