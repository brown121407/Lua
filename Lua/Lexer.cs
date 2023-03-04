namespace Lua;

internal class Lexer
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
            switch (CurrentChar)
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
                    if (Match('[') || Match('='))
                    {
                        yield return LexString();
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
                    yield return LexString();
                    break;
                default:
                    if (char.IsWhiteSpace(CurrentChar))
                    {
                        Advance(isSkipping: true);
                        continue;
                    }

                    if (char.IsLetter(CurrentChar))
                    {
                        yield return LexIdentifier();
                    }
                    else if (char.IsDigit(CurrentChar))
                    {
                        yield return LexNumber();
                    }
                    else
                    {
                        throw new LexerException($"Unexpected token: {CurrentChar}", _line, _col);
                    }

                    break;
            }

            Advance(isSkipping: true);
        }
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
        throw new NotImplementedException();
    }

    private Token LexString()
    {
        throw new NotImplementedException();
    }

    private void Advance(bool isSkipping = false)
    {
        if (IsAtEnd)
        {
            return;
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

        if (isSkipping)
        {
            _startLine = _line;
            _startCol = _col;
        }

        _pos++;
    }

    private char? Peek(int lookahead = 1)
    {
        return _pos + lookahead >= _source.Length ? null : _source[_pos + lookahead];
    }

    private bool Match(char expected)
    {
        if (Peek() == expected)
        {
            Advance();
            return true;
        }

        return false;
    }

    private Token ExtractToken(TokenType type, bool withLexeme = false)
    {
        var lexeme = withLexeme ? _source.Substring(_start, _pos - _start) : null;
        Token token;

        if (type == TokenType.Identifier && lexeme is not null && _reservedKeywords.ContainsKey(lexeme))
        {
            type = _reservedKeywords[lexeme];
            token = new Token(type, null, _startLine, _startCol); // No reason to store lexeme for keywords
        }
        else
        {
            token = new Token(type, lexeme, _line, _startCol);
        }

        _startLine = _line;
        _startCol = _col;

        return token;
    }
}