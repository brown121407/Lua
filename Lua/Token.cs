namespace Lua;

public record Token(TokenType Type, string? Lexeme, object? Literal, int Line, int Column)
{
    public bool IsKeyword => Type > TokenType.And;
}