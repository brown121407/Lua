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
        {"do", TokenType.Do},
        {"if", TokenType.If},
        {"then", TokenType.Then},
        {"elseif", TokenType.ElseIf},
        {"end", TokenType.End},
        {"break", TokenType.Break},
        {"goto", TokenType.Goto},
        {"return", TokenType.Return},
        {"function", TokenType.Function},
        {"local", TokenType.Local},
    };

    public Lexer(string source)
    {
        _source = source;
    }

    public IEnumerable<Token> Lex()
    {
        while (!IsAtEnd)
        {
            _start = _pos;
            var c = Advance(isSkipping: true) ?? throw new LexerException("Unexpected EOF", _line, _col);

            switch (c)
            {
                case ';':
                    yield return ExtractToken(TokenType.Semicolon);
                    break;
                case ',':
                    yield return ExtractToken(TokenType.Comma);
                    break;
                case ':':
                    if (Match(':'))
                    {
                        yield return ExtractToken(TokenType.ColonColon);
                    }
                    else
                    {
                        yield return ExtractToken(TokenType.Colon);
                    }

                    break;
                case '=':
                    if (Match('='))
                    {
                        yield return ExtractToken(TokenType.EqualEqual);
                    }
                    else
                    {
                        yield return ExtractToken(TokenType.Equal);
                    }

                    break;
                case '.':
                    if (Match('.'))
                    {
                        if (Match('.'))
                        {
                            yield return ExtractToken(TokenType.DotDotDot);
                        }
                        else
                        {
                            yield return ExtractToken(TokenType.DotDot);
                        }
                    }
                    else
                    {
                        yield return ExtractToken(TokenType.Dot);
                    }

                    break;
                case '+':
                    yield return ExtractToken(TokenType.Plus);
                    break;
                case '-':
                    yield return ExtractToken(TokenType.Minus);
                    break;
                case '*':
                    yield return ExtractToken(TokenType.Star);
                    break;
                case '/':
                    if (Match('/'))
                    {
                        yield return ExtractToken(TokenType.DoubleSlash);
                    }
                    else
                    {
                        yield return ExtractToken(TokenType.Slash);
                    }

                    break;
                case '^':
                    yield return ExtractToken(TokenType.Caret);
                    break;
                case '%':
                    yield return ExtractToken(TokenType.Percent);
                    break;
                case '&':
                    yield return ExtractToken(TokenType.Ampersand);
                    break;
                case '~':
                    if (Match('='))
                    {
                        yield return ExtractToken(TokenType.NotEqual);
                    }
                    else
                    {
                        yield return ExtractToken(TokenType.Tilde);
                    }

                    break;
                case '|':
                    yield return ExtractToken(TokenType.Bar);
                    break;
                case '#':
                    yield return ExtractToken(TokenType.Hash);
                    break;
                case '>':
                    if (Match('>'))
                    {
                        yield return ExtractToken(TokenType.DoubleRightAngleBracket);
                    }
                    else
                    {
                        if (Match('='))
                        {
                            yield return ExtractToken(TokenType.GreaterEqual);
                        }
                        else
                        {
                            yield return ExtractToken(TokenType.Greater);
                        }
                    }

                    break;
                case '<':
                    if (Match('<'))
                    {
                        yield return ExtractToken(TokenType.DoubleLeftAngleBracket);
                    }
                    else
                    {
                        if (Match('='))
                        {
                            yield return ExtractToken(TokenType.LessEqual);
                        }
                        else
                        {
                            yield return ExtractToken(TokenType.Less);
                        }
                    }

                    break;
                case '(':
                    yield return ExtractToken(TokenType.LeftParenthesis);
                    break;
                case ')':
                    yield return ExtractToken(TokenType.RightParenthesis);
                    break;
                case '[':
                    if (Match('['))
                    {
                        yield return LexLongString();
                    }
                    else if (Match('='))
                    {
                        yield return LexLongString(1);
                    }
                    else
                    {
                        yield return ExtractToken(TokenType.LeftBracket);
                    }

                    break;
                case ']':
                    yield return ExtractToken(TokenType.RightBracket);
                    break;
                case '{':
                    yield return ExtractToken(TokenType.LeftBrace);
                    break;
                case '}':
                    yield return ExtractToken(TokenType.RightBrace);
                    break;
                case '\'':
                case '"':
                    yield return LexShortString(c);
                    break;
                default:
                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }

                    if (char.IsLetter(c))
                    {
                        yield return LexIdentifier();
                    }
                    else if (char.IsDigit(c))
                    {
                        yield return LexNumber();
                    }
                    else
                    {
                        throw new LexerException($"Unexpected token: {CurrentChar}", _line, _col);
                    }

                    break;
            }
        }

        yield return ExtractToken(TokenType.Eof);
    }

    private Token LexIdentifier()
    {
        while (!IsAtEnd && !char.IsWhiteSpace(CurrentChar))
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
        if (IsAtEnd)
        {
            return false;
        }
        
        if (CurrentChar == expected)
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