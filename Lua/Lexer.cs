namespace Lua;

public class Lexer
{
    private readonly string _source;
    private int _start = 0;
    private int _pos = 0;
    private int _line = 0;
    private int _col = 0;
    private int _startLine = 0;
    private int _startCol = 0;

    private bool IsAtEnd => _pos >= _source.Length;
    private char CurrentChar => _source[_pos];

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

                    if (char.IsLetter(c))
                    {
                        return LexIdentifier();
                    }

                    if (char.IsDigit(c) || c == '_')
                    {
                        return LexNumber();
                    }

                    throw new LexerException($"Unexpected token: {CurrentChar}", _line, _col);
            }
        }

        return ExtractToken(TokenType.Eof);
    }

    private Token LexIdentifier()
    {
        while (!IsAtEnd && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            Advance();
        }

        return ExtractToken(TokenType.Identifier, withLexeme: true);
    }

    private Token LexNumber()
    {
        while (!IsAtEnd && char.IsDigit(CurrentChar))
        {
            Advance();
        }

        if (Match('.'))
        {
            Eat(char.IsDigit);
            while (!IsAtEnd && char.IsDigit(CurrentChar))
            {
                Advance();
            }
        }

        var lexeme = _source.Substring(_start, _pos - _start);
        if (double.TryParse(lexeme, out var literal))
        {
            return ExtractToken(TokenType.Number, literal, withLexeme: true);
        }

        throw new LexerException($"Failed to convert {lexeme} to a number", _line, _col);
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
            Advance();
        }

        if (!Match(delimiter))
        {
            throw new LexerException($"Expected {delimiter}", _line, _col);
        }

        var literal = _source.Substring(_start + 1, _pos - _start - 2);

        return ExtractToken(TokenType.String, literal, withLexeme: true);
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
                else
                {
                    possiblyClosing = true;
                    closingLevel = 0;
                }
            }
            else if (CurrentChar == '=' && possiblyClosing)
            {
                closingLevel++;
            }

            Advance();
        }

        if (!Match(']'))
        {
            throw new LexerException($"Expected ]", _line, _col);
        }
        
        var literal = _source.Substring(_start + 2 + level, _pos - 2 - level - (_start + 2 + level));

        return ExtractToken(TokenType.String, literal, withLexeme: true);
    }

    private void SkipToWhitespace()
    {
        while (Match(c => !char.IsWhiteSpace(c))) {}
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
            _col = 0;
        }
        else
        {
            _col++;
        }

        return _source[_pos++];
    }

    private char? Peek(int lookahead = 1)
    {
        return _pos + lookahead >= _source.Length ? null : _source[_pos + lookahead];
    }

    private bool Match(char expected)
    {
        return Match(c => c == expected);
    }

    private bool Match(Predicate<char> predicate)
    {
        if (IsAtEnd)
        {
            return false;
        }

        if (predicate(CurrentChar))
        {
            Advance();
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
    
    private Token ExtractToken(TokenType type, object? literal = null, bool withLexeme = false)
    {
        var lexeme = withLexeme ? _source.Substring(_start, _pos - _start) : null;
        Token token;

        if (type == TokenType.Identifier && lexeme is not null && _reservedKeywords.ContainsKey(lexeme))
        {
            type = _reservedKeywords[lexeme];
            token = new Token(type, null, null, _startLine, _startCol); // No reason to store lexeme for keywords
        }
        else
        {
            token = new Token(type, lexeme, literal, _line, _startCol);
        }

        _startLine = _line;
        _startCol = _col;

        return token;
    }
}