namespace Lua;

public class Lexer
{
    private readonly string _source;
    private int _start = 0;
    private int _pos = 0;
    private int _line = 1;
    private int _col = 1;
    private int _startLine = 1;
    private int _startCol = 1;

    private bool IsAtEnd => _pos >= _source.Length;
    private char CurrentChar => _source[_pos];
    private string CurrentLexeme => _source[_start.._pos];

    private readonly Dictionary<string, TokenType> _reservedKeywords = new()
    {
        {"and", TokenType.And},
        {"or", TokenType.Or},
        {"not", TokenType.Not},
        {"nil", TokenType.Nil},
        {"false", TokenType.False},
        {"true", TokenType.True},
        {"for", TokenType.For},
        {"while", TokenType.While},
        {"repeat", TokenType.Repeat},
        {"until", TokenType.Until},
        {"do", TokenType.Do},
        {"if", TokenType.If},
        {"then", TokenType.Then},
        {"elseif", TokenType.ElseIf},
        {"else", TokenType.Else},
        {"end", TokenType.End},
        {"break", TokenType.Break},
        {"goto", TokenType.Goto},
        {"return", TokenType.Return},
        {"function", TokenType.Function},
        {"local", TokenType.Local},
        {"in", TokenType.In}
    };

    public Lexer(string source)
    {
        _source = source;
    }

    public IEnumerable<Token> Lex(bool skipErrors = false)
    {
        Token? currentToken = null;
        do
        {
            try
            {
                currentToken = NextToken();
            }
            catch (LexerException exception)
            {
                Console.Error.WriteLine(exception.Message);
                SkipToWhitespace();
                if (skipErrors)
                {
                    continue;
                }

                throw;
            }

            yield return currentToken;
        } while (currentToken?.Type != TokenType.Eof);
    }

    private Token NextToken()
    {
        while (!IsAtEnd)
        {
            _start = _pos;
            var c = Advance(isSkipping: true) ?? throw new LexerException("Unexpected EOF", _line, _col);

            switch (c)
            {
                case ';':
                    return ExtractToken(TokenType.Semicolon);
                case ',':
                    return ExtractToken(TokenType.Comma);
                case ':':
                    if (Match(':'))
                    {
                        return ExtractToken(TokenType.ColonColon);
                    }

                    return ExtractToken(TokenType.Colon);
                case '=':
                    if (Match('='))
                    {
                        return ExtractToken(TokenType.EqualEqual);
                    }

                    return ExtractToken(TokenType.Equal);
                case '.':
                    if (Match('.'))
                    {
                        if (Match('.'))
                        {
                            return ExtractToken(TokenType.DotDotDot);
                        }

                        return ExtractToken(TokenType.DotDot);
                    }

                    return ExtractToken(TokenType.Dot);
                case '+':
                    return ExtractToken(TokenType.Plus);
                case '-':
                    if (Match('-', isSkipping: true))
                    {
                        SkipComment();
                        continue;
                    }
                    return ExtractToken(TokenType.Minus);
                case '*':
                    return ExtractToken(TokenType.Star);
                case '/':
                    if (Match('/'))
                    {
                        return ExtractToken(TokenType.DoubleSlash);
                    }

                    return ExtractToken(TokenType.Slash);
                case '^':
                    return ExtractToken(TokenType.Caret);
                case '%':
                    return ExtractToken(TokenType.Percent);
                case '&':
                    return ExtractToken(TokenType.Ampersand);
                case '~':
                    if (Match('='))
                    {
                        return ExtractToken(TokenType.NotEqual);
                    }

                    return ExtractToken(TokenType.Tilde);
                case '|':
                    return ExtractToken(TokenType.Bar);
                case '#':
                    return ExtractToken(TokenType.Hash);
                case '>':
                    if (Match('>'))
                    {
                        return ExtractToken(TokenType.DoubleRightAngleBracket);
                    }

                    if (Match('='))
                    {
                        return ExtractToken(TokenType.GreaterEqual);
                    }

                    return ExtractToken(TokenType.Greater);
                case '<':
                    if (Match('<'))
                    {
                        return ExtractToken(TokenType.DoubleLeftAngleBracket);
                    }

                    if (Match('='))
                    {
                        return ExtractToken(TokenType.LessEqual);
                    }

                    return ExtractToken(TokenType.Less);

                case '(':
                    return ExtractToken(TokenType.LeftParenthesis);
                case ')':
                    return ExtractToken(TokenType.RightParenthesis);
                case '[':
                    if (Match('['))
                    {
                        return LexLongString();
                    }

                    if (Match('='))
                    {
                        return LexLongString(1);
                    }

                    return ExtractToken(TokenType.LeftBracket);
                case ']':
                    return ExtractToken(TokenType.RightBracket);
                case '{':
                    return ExtractToken(TokenType.LeftBrace);
                case '}':
                    return ExtractToken(TokenType.RightBrace);
                case '\'':
                case '"':
                    return LexShortString(c);
                default:
                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }

                    if (char.IsLetter(c) || c == '_')
                    {
                        return LexIdentifier();
                    }

                    if (char.IsDigit(c))
                    {
                        return LexNumber(c);
                    }

                    throw new LexerException($"Unexpected token: {CurrentChar}", _line, _col);
            }
        }

        return ExtractToken(TokenType.Eof, withLexeme: false);
    }

    private Token LexIdentifier()
    {
        while (!IsAtEnd && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            Advance();
        }

        return ExtractToken(TokenType.Identifier);
    }

    private Token LexNumber(char firstDigit)
    {
        if (firstDigit == '0' && Match('x'))
        {
            return LexHexNumber();
        }

        while (!IsAtEnd && char.IsDigit(CurrentChar))
        {
            Advance();
        }

        if (Match('.'))
        {
            Eat(char.IsDigit);
            while (Match(char.IsDigit)) { }
        }

        if (Match('e') || Match('E'))
        {
            _ = Match('+') || Match('-');
            Eat(char.IsDigit);
            while (Match(char.IsDigit)) { }
        }

        var lexeme = CurrentLexeme;
        if (double.TryParse(lexeme, out var literal))
        {
            return ExtractToken(TokenType.Number, literal);
        }

        throw new LexerException($"Failed to convert {lexeme} to a number", _line, _col);
    }

    private Token LexHexNumber()
    {
        while (Match(c => char.IsDigit(c) || "abcdefABCDEF".Contains(c))) { }

        var lexeme = CurrentLexeme;
        if (int.TryParse(lexeme[2..], System.Globalization.NumberStyles.HexNumber, null, out var literal))
        {
            return ExtractToken(TokenType.Number, literal);
        }

        throw new LexerException($"Faield to convert {lexeme} to a number", _line, _col);
    }

    private Token LexShortString(char delimiter)
    {
        var escaped = false;
        while (!IsAtEnd)
        {
            if (CurrentChar == '\\')
            {
                escaped = true;
            }
            else if (CurrentChar == delimiter && !escaped)
            {
                break;
            }
            else if (escaped)
            {
                escaped = !escaped;
            }
            Advance();
        }

        if (!Match(delimiter))
        {
            throw new LexerException($"Expected {delimiter}", _line, _col);
        }

        var literal = _source.Substring(_start + 1, _pos - _start - 2);

        return ExtractToken(TokenType.String, literal);
    }

    private Token LexLongString(int startingLevel = 0)
    {
        var level = startingLevel;
        if (level > 0)
        {
            while (Match('='))
            {
                level++;
            }

            if (!Match('['))
            {
                throw new LexerException("Expected [", _line, _pos);
            }
        }

        var possiblyClosing = false;
        var closingLevel = 0;
        while (!IsAtEnd)
        {
            if (CurrentChar == ']')
            {
                if (possiblyClosing && closingLevel == level)
                {
                    break;
                }

                possiblyClosing = true;
                closingLevel = 0;
            }
            else if (CurrentChar == '=' && possiblyClosing)
            {
                closingLevel++;
            }
            else
            {
                possiblyClosing = false;
            }

            Advance();
        }

        if (!Match(']'))
        {
            throw new LexerException($"Expected ]", _line, _col);
        }

        var literal = _source.Substring(_start + 2 + level, _pos - 2 - level - (_start + 2 + level));

        return ExtractToken(TokenType.String, literal);
    }

    private void SkipComment()
    {
        var level = 0;

        if (Match('[', isSkipping: true))
        {
            while (Match('=', isSkipping: true))
            {
                level++;
            }

            if (Match('[', isSkipping: true))
            {
                var possiblyClosing = false;
                var closingLevel = 0;
                while (!IsAtEnd)
                {
                    if (CurrentChar == ']')
                    {
                        if (possiblyClosing && closingLevel == level)
                        {
                            break;
                        }

                        possiblyClosing = true;
                        closingLevel = 0;
                    }
                    else if (CurrentChar == '=' && possiblyClosing)
                    {
                        closingLevel++;
                    }
                    else
                    {
                        possiblyClosing = false;
                    }

                    Advance(isSkipping: true);
                }

                Advance(isSkipping: true);
            }
            else
            {
                SkipShortComment();
            }
        }
        else
        {
            SkipShortComment();
        }

        _startLine = _line;
        _startCol = _col;
    }

    private void SkipShortComment()
    {
        while (Match(c => c != '\n', isSkipping: true)) {}
    }

    private void SkipToWhitespace()
    {
        while (Match(c => !char.IsWhiteSpace(c), isSkipping: true)) {}
    }

    private char? Advance(bool isSkipping = false)
    {
        if (IsAtEnd)
        {
            return null;
        }

        if (isSkipping)
        {
            _startLine = _line;
            _startCol = _col;
        }

        if (CurrentChar == '\n')
        {
            _line++;
            _col = 1;
        }
        else
        {
            _col++;
        }

        return _source[_pos++];
    }

    private bool Match(char expected, bool isSkipping = false)
    {
        return Match(c => c == expected, isSkipping);
    }

    private bool Match(Predicate<char> predicate, bool isSkipping = false)
    {
        if (IsAtEnd)
        {
            return false;
        }

        if (predicate(CurrentChar))
        {
            Advance(isSkipping);
            return true;
        }

        return false;
    }

    private void Eat(Predicate<char> predicate)
    {
        if (IsAtEnd)
        {
            throw new LexerException($"Unexpected EOF", _line, _col);
        }

        if (!predicate(CurrentChar))
        {
            throw new LexerException($"Unexpected {CurrentChar}", _line, _col);
        }

        Advance();
    }

    private Token ExtractToken(TokenType type, object? literal = null, bool withLexeme = true)
    {
        var lexeme = _source[_start.._pos];
        Token token;

        if (type == TokenType.Identifier && _reservedKeywords.ContainsKey(lexeme))
        {
            type = _reservedKeywords[lexeme];
        }

        token = new Token(type, withLexeme ? lexeme : string.Empty, literal, _line, _startCol);

        _startLine = _line;
        _startCol = _col;

        return token;
    }
}