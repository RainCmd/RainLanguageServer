using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage
{
    internal static class FileParse
    {
        private static void CheckEnd(TextRange segment, int index, MessageCollector collector)
        {
            if (Lexical.TryAnalysis(segment, index, out var lexical, collector) && lexical.type != LexicalType.Annotation)
                collector.Add(lexical.anchor, ErrorLevel.Error, "新的语句或声明应另起一行");
        }
        private static void CheckEnd(TextRange segment, TextPosition index, MessageCollector collector) => CheckEnd(segment, index - segment.start, collector);
        private static TextPosition GetLastLexicalEnd(TextRange segment, TextPosition start)
        {
            while (Lexical.TryAnalysis(segment, start, out var lexical, null)) start = lexical.anchor.end;
            return start;
        }
        private static TextRange TrimLine(TextLine line, TextPosition start)
        {
            if (line.indent < 0) return line.start & line.start;
            return line.start & GetLastLexicalEnd(line, start);
        }
        private static bool TryParseAttribute(TextLine line, List<TextRange> attributes, MessageCollector collector)
        {
            if (Lexical.TryAnalysis(line, 0, out var bracket, collector) && bracket.type == LexicalType.BracketLeft1)
            {
                var lexical = bracket;
                while (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector) && lexical.type == LexicalType.ConstString)
                {
                    attributes.Add(lexical.anchor);
                    if (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector))
                    {
                        if (lexical.type == LexicalType.BracketRight1) CheckEnd(line, lexical.anchor.end, collector);
                        else if (lexical.type == LexicalType.Comma || lexical.type == LexicalType.Semicolon) continue;
                        else collector.Add(lexical.anchor, ErrorLevel.Error, "应输入 , 或 ;");
                    }
                    else collector.Add(bracket.anchor, ErrorLevel.Error, "缺少 ]");
                    return true;
                }
                if (lexical.anchor.Valid) collector.Add(lexical.anchor, ErrorLevel.Error, "应输入字符串常量");
                else collector.Add(bracket.anchor, ErrorLevel.Error, "需要输入属性");
            }
            return false;
        }
        private static void DiscardAttributes(List<TextRange> attributes, MessageCollector collector)
        {
            foreach (var attribute in attributes)
                collector.Add(attribute, ErrorLevel.Warning, "属性被丢弃");
            attributes.Clear();
        }
        private static Visibility ParseVisibility(TextLine line, out TextPosition index, MessageCollector collector)
        {
            var result = Visibility.None;
            index = line.start;
            while (Lexical.TryAnalysis(line, index, out var lexical, collector) && lexical.type == LexicalType.Word)
            {
                var visibility = Visibility.None;
                if (lexical.anchor == KeyWords.PUBLIC) visibility = Visibility.Public;
                else if (lexical.anchor == KeyWords.INTERNAL) visibility = Visibility.Internal;
                else if (lexical.anchor == KeyWords.SPACE) visibility = Visibility.Space;
                else if (lexical.anchor == KeyWords.PROTECTED) visibility = Visibility.Protected;
                else if (lexical.anchor == KeyWords.PRIVATE) visibility = Visibility.Private;
                else break;
                if (result == visibility) collector.Add(lexical.anchor, ErrorLevel.Warning, "重复的可访问性修饰符");
                else if (result == Visibility.None) result = visibility;
                else collector.Add(lexical.anchor, ErrorLevel.Error, "无效的可访问性修饰符");
                index = lexical.anchor.end;
            }
            return result;
        }
        private static bool TryParseVariable(TextLine line, TextPosition position, out TextRange name, [MaybeNullWhen(false)] out FileType type, out TextRange? expression, MessageCollector collector)
        {
            name = default;
            type = default;
            expression = default;
            if (Lexical.TryExtractName(line, position, out var names, collector))
            {
                var qualifiedName = new QualifiedName(names);
                position = qualifiedName.name.end;
                var dimension = Lexical.ExtractDimension(line, ref position);
                type = new FileType(qualifiedName.Range.start & position, qualifiedName, dimension);
                if (Lexical.TryAnalysis(line, position, out var lexical, collector) && lexical.type == LexicalType.Word)
                {
                    name = lexical.anchor;
                    if (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector))
                    {
                        if (lexical.type == LexicalType.BracketLeft0) return false;
                        else if (lexical.type == LexicalType.Assignment)
                        {
                            if (Lexical.TryAnalysis(line, lexical.anchor.end, out var expressionLexical, collector))
                            {
                                expression = expressionLexical.anchor.start & GetLastLexicalEnd(line, expressionLexical.anchor.end);
                                return true;
                            }
                            else collector.Add(lexical.anchor, ErrorLevel.Error, "应输入表达式");
                        }
                        else collector.Add(lexical.anchor, ErrorLevel.Error, "应输入 =");
                    }
                    return true;
                }
            }
            return false;
        }
        private static FileEnum.Element ParseEnumElement(FileSpace space, TextLine line, TextRange name)
        {
            if (Lexical.TryAnalysis(line, name.end, out var lexical, space.collector))
            {
                if (lexical.type != LexicalType.Assignment) space.collector.Add(lexical.anchor, ErrorLevel.Error, "应输入 =");
                else if (Lexical.TryAnalysis(line, lexical.anchor.end, out var expressionLexical, space.collector))
                {
                    var range = TrimLine(line, lexical.anchor.end);
                    return new FileEnum.Element(space, name, expressionLexical.anchor.start & range.end) { range = range };
                }
                else space.collector.Add(lexical.anchor, ErrorLevel.Error, "缺少表达式");
            }
            return new FileEnum.Element(space, name, null) { range = name };
        }
        private static void ParseTuple(TextRange segment, TextPosition position, out List<FileType> tuple, MessageCollector collector)
        {
            tuple = [];
        label_parse_type:
            if (Lexical.TryExtractName(segment, position, out var names, collector))
            {
                var qualifiedName = new QualifiedName(names);
                position = qualifiedName.name.end;
                var dimension = Lexical.ExtractDimension(segment, ref position);
                tuple.Add(new FileType(qualifiedName.Range.start & position, qualifiedName, dimension));
                if (Lexical.TryAnalysis(segment, position, out var lexical, collector))
                {
                    if (lexical.type != LexicalType.Comma && lexical.type != LexicalType.Semicolon)
                        collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入 , 或 ;");
                    position = lexical.anchor.end;
                    goto label_parse_type;
                }
            }
            else if (Lexical.TryAnalysis(segment, position, out var lexical, collector))
            {
                collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入类型");
                position = lexical.anchor.end;
                goto label_parse_type;
            }
            else if (tuple.Count > 0) collector.Add((position - 1) & position, ErrorLevel.Error, "需要输入类型");
        }
        private static bool TryParseTupleDeclaration(TextLine line, TextPosition position, [MaybeNullWhen(false)] out List<FileType> tuple, out TextRange name, MessageCollector collector)
        {
            name = default;
            var index = position;
            while (Lexical.TryAnalysis(line, index, out var lexical, collector))
            {
                if (lexical.type == LexicalType.BracketLeft0) break;
                name = lexical.anchor;
                index = lexical.anchor.end;
            }
            if (name.Valid)
            {
                ParseTuple(position & name.start, position, out tuple, collector);
                return true;
            }
            tuple = null;
            return false;
        }
        private static TextRange ParseParameters(TextRange segment, TextPosition position, out List<FileParameter> parameters, MessageCollector collector)
        {
            parameters = [];
            if (!Lexical.TryAnalysis(segment, position, out var lexical, collector))
            {
                collector.Add((position - 1) & position, ErrorLevel.Error, "需要输入 (");
                return position & position;
            }
            if (lexical.type != LexicalType.BracketLeft0)
            {
                collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入 (");
                return lexical.anchor.start & lexical.anchor.start;
            }
            Lexical.MatchBlock(lexical.anchor.start & segment.end, LexicalType.BracketLeft0, LexicalType.BracketRight0, out var bracketLeft, out var bracketRight, collector);
            segment = bracketLeft.end & bracketRight.start;

        label_parse_parameter:
            position = lexical.anchor.end;
            while (Lexical.TryExtractName(segment, position, out var names, collector))
            {
                var qualifiedName = new QualifiedName(names);
                position = qualifiedName.name.end;
                var dimension = Lexical.ExtractDimension(segment, ref position);
                var type = new FileType(qualifiedName.Range.start & position, qualifiedName, dimension);
                var parameter = new FileParameter(type, position & position);
            label_try_parse_name:
                if (Lexical.TryAnalysis(segment, position, out lexical, collector))
                {
                    position = lexical.anchor.end;
                    if (lexical.type == LexicalType.Word)
                    {
                        parameter = new FileParameter(type, lexical.anchor);
                        goto label_try_parse_name;
                    }
                    else
                    {
                        parameters.Add(parameter);
                        if (lexical.type != LexicalType.Comma && lexical.type != LexicalType.Semicolon)
                            collector.Add(lexical.anchor, ErrorLevel.Error, "应输入 , 或 ;");
                    }
                }
                else
                {
                    parameters.Add(parameter);
                    return bracketLeft & bracketRight;
                }
            }
            if (Lexical.TryAnalysis(segment, position, out lexical, collector))
            {
                collector.Add(lexical.anchor, ErrorLevel.Error, "应输入类型");
                goto label_parse_parameter;
            }
            return bracketLeft & bracketRight;
        }
        private static void ParseBlock(LineReader reader, int parentIndent, List<TextLine> lines, List<TextLine> annotations)
        {
            while (reader.ReadLine(out var line))
            {
                if (line.indent == TextLine.ANNOTATION) annotations.Add(line);
                else if (line.indent == TextLine.EMPTY) annotations.Clear();
                else if (line.indent > parentIndent)
                {
                    annotations.Clear();
                    lines.Add(line);
                }
                else if (line.indent >= 0)
                {
                    reader.Rollback();
                    break;
                }
            }
        }
        private static void ParseInherits(TextLine line, TextPosition position, List<FileType> inherits, MessageCollector collector)
        {
        label_parse_inherit:
            while (Lexical.TryExtractName(line, position, out var names, collector))
            {
                var qualifiedName = new QualifiedName(names);
                position = qualifiedName.name.end;
                var dimension = Lexical.ExtractDimension(line, ref position);
                var type = new FileType(qualifiedName.Range.start & position, qualifiedName, dimension);
                inherits.Add(type);
                if (dimension > 0) collector.Add(type.range, ErrorLevel.Error, "数组类型不能被继承");
            }
            if (Lexical.TryAnalysis(line, position, out var lexical, collector))
            {
                collector.Add(lexical.anchor, ErrorLevel.Error, "应输入类型");
                position = lexical.anchor.end;
                goto label_parse_inherit;
            }
        }
        private static void ParseDeclaration<T>(FileSpace space, LineReader reader, TextLine line, TextRange category,
            Func<TextRange, T> creater, Action<T, TextLine>? memberParser,
            List<TextRange> attributes, List<TextLine> annotations)
            where T : FileDeclaration
        {
            if (Lexical.TryAnalysis(line, category.end, out var fileName, space.collector))
            {
                if (fileName.type != LexicalType.Word) space.collector.Add(fileName.anchor, ErrorLevel.Error, "不是有效的标识符名称");
                var file = creater(fileName.anchor);
                file.range = TrimLine(line, fileName.anchor.end);
                file.attributes.AddRange(attributes);
                attributes.Clear();
                file.annotation.AddRange(annotations);
                annotations.Clear();
                if (memberParser == null) return;
                var indent = -1; var parentIndent = line.indent;
                while (reader.ReadLine(out line))
                {
                    if (line.indent == TextLine.ANNOTATION) annotations.Add(line);
                    else if (line.indent == TextLine.EMPTY) annotations.Clear();
                    else if (TryParseAttribute(line, attributes, space.collector)) continue;
                    else
                    {
                        if (indent < 0)
                        {
                            indent = line.indent;
                            if (indent <= parentIndent) break;
                        }
                        else if (line.indent > indent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                        else if (line.indent < indent)
                        {
                            if (line.indent > parentIndent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                            else break;
                        }
                        memberParser(file, line);
                    }
                }
                reader.Rollback();
                file.range &= reader.GetLastValidLine();
            }
            else space.collector.Add(category, ErrorLevel.Error, "应输入标识符");
        }
        private static void ParseDeclaration(FileSpace space, LineReader reader, TextLine line, List<TextRange> attributes, List<TextLine> annotations)
        {
            var visibility = ParseVisibility(line, out var index, space.collector);
            if (!Lexical.TryAnalysis(line, index, out var lexical, space.collector))
            {
                space.collector.Add((index - 1) & index, ErrorLevel.Error, "应输入标识符");
                return;
            }
            if (lexical.type != LexicalType.Word && !lexical.type.IsReloadable())
            {
                space.collector.Add(lexical.anchor, ErrorLevel.Error, "不是有效的标识符");
                return;
            }

            if (visibility == Visibility.None) visibility = Visibility.Private;

            if (lexical.anchor == KeyWords.CONST)
            {
                if (TryParseVariable(line, lexical.anchor.end, out var name, out var type, out var expression, space.collector))
                {
                    if (expression == null) space.collector.Add(name, ErrorLevel.Error, "常量必须在声明时赋值");
                    var variable = new FileVariable(space, visibility, name, true, type, expression) { range = TrimLine(line, name.end) };
                    variable.attributes.AddRange(attributes);
                    attributes.Clear();
                    variable.annotation.AddRange(annotations);
                    annotations.Clear();
                    space.variables.Add(variable);
                    return;
                }
                space.collector.Add(lexical.anchor, ErrorLevel.Error, "const 关键字只能修饰全局变量");
                if (!Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, space.collector) || (lexical.type != LexicalType.Word && !lexical.type.IsReloadable())) return;
            }
            if (lexical.anchor == KeyWords.ENUM)
            {
                ParseDeclaration(space, reader, line, lexical.anchor,
                    name =>
                    {
                        var file = new FileEnum(space, visibility, name);
                        CheckEnd(line, name.end, space.collector);
                        space.enums.Add(file);
                        return file;
                    },
                    (file, memberLine) =>
                    {
                        if (Lexical.TryAnalysis(memberLine, 0, out var memberLexical, space.collector))
                        {
                            if (memberLexical.type == LexicalType.Word)
                            {
                                var result = ParseEnumElement(space, memberLine, memberLexical.anchor);
                                result.annotation.AddRange(annotations);
                                annotations.Clear();
                                file.elements.Add(result);
                            }
                            else space.collector.Add(memberLexical.anchor, ErrorLevel.Error, "应输入枚举名");
                        }
                        else space.collector.Add(lexical.anchor, ErrorLevel.Error, "应输入枚举名");
                    }, attributes, annotations);
            }
            else if (lexical.anchor == KeyWords.STRUCT)
            {
                ParseDeclaration(space, reader, line, lexical.anchor,
                    name =>
                    {
                        var file = new FileStruct(space, visibility, name);
                        CheckEnd(line, name.end, space.collector);
                        space.structs.Add(file);
                        return file;
                    },
                    (file, memberLine) =>
                    {
                        var visibility = ParseVisibility(memberLine, out var index, space.collector);
                        if (TryParseVariable(memberLine, index, out var name, out var type, out var expression, space.collector))
                        {
                            if (visibility != Visibility.None) space.collector.Add(name, ErrorLevel.Error, "结构体成员字段不允许有访问修饰符");
                            var member = new FileStruct.Variable(space, name, type) { range = TrimLine(memberLine, name.end) };
                            member.attributes.AddRange(attributes);
                            attributes.Clear();
                            member.annotation.AddRange(annotations);
                            annotations.Clear();
                            file.variables.Add(member);
                        }
                        else
                        {
                            if (visibility == Visibility.None) visibility = Visibility.Private;
                            if (TryParseTupleDeclaration(memberLine, index, out var tuple, out var memberName, space.collector))
                            {
                                var parameterRange = ParseParameters(memberLine, memberName.end, out var parameters, space.collector);
                                if (parameterRange.Count > 0)
                                {
                                    CheckEnd(memberLine, parameterRange.end, space.collector);
                                    var member = new FileStruct.Function(space, visibility, memberName, parameters, tuple) { range = TrimLine(memberLine, parameterRange.end) };
                                    member.attributes.AddRange(attributes);
                                    attributes.Clear();
                                    member.annotation.AddRange(annotations);
                                    annotations.Clear();
                                    file.functions.Add(member);
                                    ParseBlock(reader, memberLine.indent, member.body, annotations);
                                    member.range &= reader.GetLastValidLine();
                                }
                            }
                            else space.collector.Add((index - 1) & index, ErrorLevel.Error, "需要输入标识符");
                        }
                    }, attributes, annotations);
            }
            else if (lexical.anchor == KeyWords.CLASS)
            {
                ParseDeclaration(space, reader, line, lexical.anchor,
                    name =>
                    {
                        var file = new FileClass(space, visibility, name);
                        ParseInherits(line, name.end, file.inherits, space.collector);
                        space.classes.Add(file);
                        return file;
                    },
                    (file, memberLine) =>
                    {
                        var visibility = ParseVisibility(memberLine, out var index, space.collector);
                        if (TryParseVariable(memberLine, index, out var name, out var type, out var expression, space.collector))
                        {
                            if (visibility == Visibility.None) visibility = Visibility.Private;
                            var member = new FileClass.Variable(space, visibility, name, type, expression) { range = TrimLine(memberLine, name.end) };
                            member.attributes.AddRange(attributes);
                            attributes.Clear();
                            member.annotation.AddRange(annotations);
                            annotations.Clear();
                            file.variables.Add(member);
                        }
                        else if (Lexical.TryAnalysis(memberLine, index, out var lexical, space.collector))
                        {
                            if (lexical.type == LexicalType.Negate)
                            {
                                CheckEnd(memberLine, lexical.anchor.end, space.collector);
                                if (visibility != Visibility.None) space.collector.Add(lexical.anchor, ErrorLevel.Warning, "析构函数的可访问性修饰符无效");
                                if (file.descontructor == null) file.descontructor = new FileClass.Descontructor(lexical.anchor) { range = TrimLine(memberLine, lexical.anchor.end) };
                                else
                                {
                                    var message = new Message(lexical.anchor, ErrorLevel.Error, "已经声明了析构函数");
                                    message.AddRelated(file.descontructor.name, "已经声明的析构函数");
                                    space.collector.Add(message);
                                }
                                DiscardAttributes(attributes, space.collector);
                                annotations.Clear();
                                ParseBlock(reader, memberLine.indent, file.descontructor.body, annotations);
                                file.descontructor.range &= reader.GetLastValidLine();
                            }
                            else
                            {
                                if (visibility == Visibility.None) visibility = Visibility.Private;
                                if (TryParseTupleDeclaration(memberLine, index, out var tuple, out var memberName, space.collector))
                                {
                                    var parameterRange = ParseParameters(memberLine, memberName.end, out var parameters, space.collector);
                                    if (parameterRange.Count > 0)
                                    {
                                        if (tuple.Count == 0 && memberName.ToString() == file.name)
                                        {
                                            var member = new FileClass.Constructor(space, visibility, memberName, parameters, tuple, parameterRange.end & memberLine.end) { range = TrimLine(memberLine, parameterRange.end) };
                                            member.attributes.AddRange(attributes);
                                            attributes.Clear();
                                            member.annotation.AddRange(annotations);
                                            annotations.Clear();
                                            file.constructors.Add(member);
                                            ParseBlock(reader, memberLine.indent, member.body, annotations);
                                            member.range &= reader.GetLastValidLine();
                                        }
                                        else
                                        {
                                            CheckEnd(memberLine, parameterRange.end, space.collector);
                                            var member = new FileClass.Function(space, visibility, memberName, parameters, tuple) { range = TrimLine(memberLine, parameterRange.end) };
                                            member.attributes.AddRange(attributes);
                                            attributes.Clear();
                                            member.annotation.AddRange(annotations);
                                            annotations.Clear();
                                            file.functions.Add(member);
                                            ParseBlock(reader, memberLine.indent, member.body, annotations);
                                            member.range &= reader.GetLastValidLine();
                                        }
                                    }
                                }
                                else space.collector.Add((index - 1) & index, ErrorLevel.Error, "需要输入标识符");
                            }
                        }
                    }, attributes, annotations);

            }
            else if (lexical.anchor == KeyWords.INTERFACE)
            {
                ParseDeclaration(space, reader, line, lexical.anchor,
                    name =>
                    {
                        var file = new FileInterface(space, visibility, name);
                        ParseInherits(line, name.end, file.inherits, space.collector);
                        space.interfaces.Add(file);
                        return file;
                    },
                    (file, memberLine) =>
                    {
                        var visibility = ParseVisibility(memberLine, out var index, space.collector);
                        if (TryParseTupleDeclaration(memberLine, index, out var tuple, out var memberName, space.collector))
                        {
                            if (visibility != Visibility.None) space.collector.Add(memberName, ErrorLevel.Error, "接口函数不允许有可访问性修饰符");
                            var parameterRange = ParseParameters(memberLine, memberName.end, out var parameters, space.collector);
                            CheckEnd(memberLine, parameterRange.end, space.collector);
                            var member = new FileInterface.Function(space, Visibility.None, memberName, parameters, tuple) { range = TrimLine(memberLine, parameterRange.end) };
                            member.attributes.AddRange(attributes);
                            attributes.Clear();
                            member.annotation.AddRange(annotations);
                            annotations.Clear();
                            file.functions.Add(member);
                        }
                        else space.collector.Add((index - 1) & index, ErrorLevel.Error, "需要输入标识符");
                    }, attributes, annotations);
            }
            else if (lexical.anchor == KeyWords.DELEGATE)
            {
                if (TryParseTupleDeclaration(line, lexical.anchor.end, out var tuple, out var name, space.collector))
                {
                    var parameterRange = ParseParameters(line, name.end, out var parameters, space.collector);
                    CheckEnd(line, parameterRange.end, space.collector);
                    var file = new FileDelegate(space, visibility, name, parameters, tuple) { range = TrimLine(line, parameterRange.end) };
                    file.attributes.AddRange(attributes);
                    attributes.Clear();
                    file.annotation.AddRange(annotations);
                    annotations.Clear();
                    space.delegates.Add(file);
                }
                else space.collector.Add((lexical.anchor.end - 1) & lexical.anchor.end, ErrorLevel.Error, "需要输入标识符");
            }
            else if (lexical.anchor == KeyWords.TASK)
            {
                if (TryParseTupleDeclaration(line, lexical.anchor.end, out var tuple, out var name, space.collector))
                {
                    CheckEnd(line, name.end, space.collector);
                    var file = new FileTask(space, visibility, name, tuple) { range = TrimLine(line, name.end) };
                    file.attributes.AddRange(attributes);
                    attributes.Clear();
                    file.annotation.AddRange(annotations);
                    annotations.Clear();
                    space.tasks.Add(file);
                }
                else space.collector.Add((lexical.anchor.end - 1) & lexical.anchor.end, ErrorLevel.Error, "需要输入标识符");
            }
            else if (lexical.anchor == KeyWords.NATIVE)
            {
                if (TryParseTupleDeclaration(line, lexical.anchor.end, out var tuple, out var name, space.collector))
                {
                    var parameterRange = ParseParameters(line, name.end, out var parameters, space.collector);
                    CheckEnd(line, parameterRange.end, space.collector);
                    var file = new FileNative(space, visibility, name, parameters, tuple) { range = TrimLine(line, parameterRange.end) };
                    file.attributes.AddRange(attributes);
                    attributes.Clear();
                    file.annotation.AddRange(annotations);
                    annotations.Clear();
                    space.natives.Add(file);
                }
                else space.collector.Add((lexical.anchor.end - 1) & lexical.anchor.end, ErrorLevel.Error, "需要输入标识符");
            }
            else if (TryParseVariable(line, lexical.anchor.start, out var name, out var type, out var expression, space.collector))
            {
                var file = new FileVariable(space, visibility, name, false, type, expression) { range = TrimLine(line, expression != null ? expression.Value.end : name.end) };
                file.attributes.AddRange(attributes);
                attributes.Clear();
                file.annotation.AddRange(annotations);
                annotations.Clear();
                space.variables.Add(file);
            }
            else if (TryParseTupleDeclaration(line, lexical.anchor.start, out var tuple, out name, space.collector))
            {
                var parameterRange = ParseParameters(line, name.end, out var parameters, space.collector);
                if (parameterRange.Count > 0)
                {
                    CheckEnd(line, parameterRange.end, space.collector);
                    var file = new FileFunction(space, visibility, name, parameters, tuple) { range = TrimLine(line, parameterRange.end) };
                    file.attributes.AddRange(attributes);
                    attributes.Clear();
                    file.annotation.AddRange(annotations);
                    annotations.Clear();
                    ParseBlock(reader, line.indent, file.body, annotations);
                    file.range &= reader.GetLastValidLine();
                    space.functions.Add(file);
                }
            }
            else space.collector.Add((lexical.anchor.end - 1) & lexical.anchor.end, ErrorLevel.Error, "需要输入标识符");
        }
        public static FileSpace ParseSpace(AbstractLibrary library, TextDocument document)
        {
            var collector = new MessageCollector();
            var reader = new LineReader(document);
            var attributes = new List<TextRange>();
            while (reader.ReadLine(out var line))
            {
                if (line.indent < 0) continue;
                if (TryParseAttribute(line, attributes, collector)) continue;
                if (!Lexical.TryAnalysis(line, 0, out var lexical, collector)) continue;
                if (lexical.type != LexicalType.Word || lexical.anchor != KeyWords.NAMESPACE) break;
                if (!Lexical.TryExtractName(line, lexical.anchor.end, out var name, collector)) break;
                FileSpace? space = null;
                foreach (var spaceName in name)
                {
                    if (space == null) space = new FileSpace(spaceName, null, library, document, collector);
                    else space = new FileSpace(spaceName, space, space.space.GetChild(spaceName.ToString()), document, collector);
                    space.range = line;
                }
                space!.space.attributes.AddRange(attributes);
                ParseSpace(space, reader, line.indent);
                space.range &= reader.GetLastValidLine();
                while (space.parent != null) space = space.parent;
                return space;
            }
            throw new Exception($"文件( {document.path} )解析失败：最外层命名空间名与库名不一致");
        }
        public static void ParseSpace(FileSpace space, LineReader reader, int parentIndent)
        {
            var attributes = new List<TextRange>();
            var annotations = new List<TextLine>();
            var indent = -1;
            while (reader.ReadLine(out var line))
            {
                if (line.indent == TextLine.ANNOTATION) annotations.Add(line);
                else if (line.indent == TextLine.EMPTY) annotations.Clear();
                else if (TryParseAttribute(line, attributes, space.collector)) continue;
                else if (Lexical.TryAnalysis(line, 0, out var lexical, space.collector))
                {
                    if (indent < 0)
                    {
                        indent = line.indent;
                        if (indent <= parentIndent)
                        {
                            reader.Rollback();
                            break;
                        }
                    }
                    else if (line.indent > indent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                    else if (line.indent < indent)
                    {
                        if (parentIndent < 0 || line.indent > parentIndent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                        else
                        {
                            reader.Rollback();
                            break;
                        }
                    }
                    if (lexical.type == LexicalType.Word)
                    {
                        if (lexical.anchor == KeyWords.IMPORT)
                        {
                            foreach (var attribute in attributes)
                                space.collector.Add(attribute, ErrorLevel.Error, "无效的属性");

                            if (Lexical.TryExtractName(line, lexical.anchor.end, out var names, space.collector))
                            {
                                CheckEnd(line, names[^1].end, space.collector);
                                space.imports.Add(new ImportSpaceInfo(names));
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入命名空间名");
                        }
                        else if (lexical.anchor == KeyWords.NAMESPACE)
                        {
                            if (Lexical.TryExtractName(line, lexical.anchor.end, out var names, space.collector))
                            {
                                CheckEnd(line, names[^1].end, space.collector);
                                var index = space;
                                foreach (var spaceName in names)
                                    index = new FileSpace(spaceName, index, index.space.GetChild(spaceName.ToString()), space.document, space.collector) { range = line };
                                index.space.attributes.AddRange(attributes);
                                attributes.Clear();
                                ParseSpace(index, reader, line.indent);
                                index.range &= reader.GetLastValidLine();
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入命名空间名");
                        }
                        else ParseDeclaration(space, reader, line, attributes, annotations);
                    }
                    else if (lexical.type.IsReloadable())
                    {
                        if (TryParseTupleDeclaration(line, lexical.anchor.end, out var tuple, out var name, space.collector))
                        {
                            var parameterRange = ParseParameters(line, name.end, out var parameters, space.collector);
                            CheckEnd(line, parameterRange.end, space.collector);
                            var file = new FileFunction(space, Visibility.Private, name, parameters, tuple);
                            file.attributes.AddRange(attributes);
                            attributes.Clear();
                            file.annotation.AddRange(annotations);
                            annotations.Clear();
                            space.functions.Add(file);
                        }
                        else space.collector.Add((lexical.anchor.end - 1) & lexical.anchor.end, ErrorLevel.Error, "需要输入标识符");
                    }
                    else space.collector.Add(lexical.anchor, ErrorLevel.Error, "应输入类型或命名空间");
                    DiscardAttributes(attributes, space.collector);
                }
            }
            space.space.attributes.AddRange(attributes);
        }
    }
}
