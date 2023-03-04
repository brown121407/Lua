namespace Lua;

internal record Token(TokenType Type, string? Lexeme, int Line, int Column)
{
    public bool IsKeyword => Type > TokenType.And;
}