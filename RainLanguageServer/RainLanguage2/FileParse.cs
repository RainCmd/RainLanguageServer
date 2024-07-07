using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace RainLanguageServer.RainLanguage2
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
        private static TextRange TrimLine(TextLine line)
        {
            if (line.indent < 0) return line;
            var start = line.start + line.indent;
            return start & GetLastLexicalEnd(line, start);
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
            if (Lexical.TryExtractName(line, position, out var qualifiedName, collector))
            {
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
                    expression = default;
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
                if (Lexical.TryAnalysis(line, lexical.anchor.end, out var expressionLexical, space.collector))
                    return new FileEnum.Element(space, name, expressionLexical.anchor.start & line.end) { range = TrimLine(line) };
                else if (lexical.type == LexicalType.Assignment)
                    space.collector.Add(lexical.anchor, ErrorLevel.Error, "缺少表达式");
            }
            return new FileEnum.Element(space, name, null) { range = name };
        }
        private static void ParseDeclaration<T>(FileSpace space, LineReader reader, TextLine line, TextRange category,
            Func<TextRange, T> creater, Func<T, TextLine, FileDeclaration?>? memberParser,
            List<TextRange> attributes, List<TextLine> annotations)
            where T : FileDeclaration
        {
            if (Lexical.TryAnalysis(line, category.end, out var fileName, space.collector))
            {
                if (fileName.type != LexicalType.Word) space.collector.Add(fileName.anchor, ErrorLevel.Error, "不是有效的标识符名称");
                var file = creater(fileName.anchor);
                file.range = TrimLine(line);
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
                        var member = memberParser(file, line);
                        member?.annotation.AddRange(annotations);
                        annotations.Clear();
                    }
                }
                file.range &= reader.GetLastValidLine();
                reader.Rollback();
            }
            else space.collector.Add(category, ErrorLevel.Error, "应输入标识符");
        }
        private static void ParseDeclaration(FileSpace space, LineReader reader, TextLine line, List<TextRange> attributes, List<TextLine> annotations)
        {
            var visibility = ParseVisibility(line, out var index, space.collector);
            if (!Lexical.TryAnalysis(line, index, out var lexical, space.collector))
            {
                space.collector.Add(line[line.indent..index.charactor], ErrorLevel.Error, "应输入标识符");
                return;
            }
            if (lexical.type != LexicalType.Word && !lexical.IsReloadable)
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
                    var variable = new FileVariable(space, visibility, name, true, type, expression) { range = TrimLine(line) };
                    variable.attributes.AddRange(attributes);
                    attributes.Clear();
                    variable.annotation.AddRange(annotations);
                    space.variables.Add(variable);
                    return;
                }
                space.collector.Add(lexical.anchor, ErrorLevel.Error, "const 关键字只能修饰全局变量");
                if (!Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, space.collector) || (lexical.type != LexicalType.Word && !lexical.IsReloadable)) return;
            }
            if (lexical.anchor == KeyWords.ENUM)
            {
                ParseDeclaration(space, reader, line, lexical.anchor,
                    name =>
                    {
                        var result = new FileEnum(space, visibility, name);
                        CheckEnd(line, name.end, space.collector);
                        return result;
                    },
                    (file, memberLine) =>
                    {
                        if (Lexical.TryAnalysis(memberLine, 0, out var memberLexical, space.collector))
                        {
                            if (memberLexical.type == LexicalType.Word)
                            {
                                var result = ParseEnumElement(space, line, memberLexical.anchor);
                                file.elements.Add(result);
                                return result;
                            }
                            else space.collector.Add(memberLexical.anchor, ErrorLevel.Error, "应输入枚举名");
                        }
                        else space.collector.Add(lexical.anchor, ErrorLevel.Error, "应输入枚举名");
                        return null;
                    }, attributes, annotations);
            }
            else if (lexical.anchor == KeyWords.STRUCT)
            {
                ParseDeclaration(space, reader, line, lexical.anchor,
                    name =>
                    {
                        var result = new FileStruct(space, visibility, name);
                        CheckEnd(line, name.end, space.collector);
                        return result;
                    },
                    (file, memberLine) =>
                    {
                        var visibility = ParseVisibility(line, out var index, space.collector);
                        if (TryParseVariable(memberLine, index, out var name, out var type, out var expression, space.collector))
                        {
                            if (visibility != Visibility.None) space.collector.Add(name, ErrorLevel.Error, "结构体成员字段不允许有访问修饰符");
                            var member = new FileStruct.Variable(space, name, type) { range = TrimLine(line) };
                            member.attributes.AddRange(attributes);
                            attributes.Clear();
                            file.variables.Add(member);
                            return member;
                        }
                        else
                        {
                            if (visibility == Visibility.None) visibility = Visibility.Private;
                            //todo 解析成员函数
                        }
                        return null;
                    }, attributes, annotations);
            }
            else if (lexical.anchor == KeyWords.CLASS)
            {

            }
            else if (lexical.anchor == KeyWords.INTERFACE)
            {

            }
            else if (lexical.anchor == KeyWords.DELEGATE)
            {

            }
            else if (lexical.anchor == KeyWords.TASK)
            {

            }
            else if (lexical.anchor == KeyWords.NATIVE)
            {

            }
            else
            {
                if (TryParseVariable(line, lexical.anchor.end, out var name, out var type, out var expression, space.collector))
                {

                }
                else
                {

                }
            }
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
                        if (indent <= parentIndent) break;
                    }
                    else if (line.indent > indent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                    else if (line.indent < indent)
                    {
                        if (parentIndent < 0 || line.indent > parentIndent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                        else
                        {
                            space.space.attributes.AddRange(attributes);
                            break;
                        }
                    }
                    if (lexical.type == LexicalType.Word)
                    {
                        if (lexical.anchor == KeyWords.IMPORT)
                        {
                            foreach (var attribute in attributes)
                                space.collector.Add(attribute, ErrorLevel.Error, "无效的属性");

                            if (Lexical.TryExtractName(line, lexical.anchor.end, out var name, space.collector))
                            {
                                CheckEnd(line, name.name.end, space.collector);
                                name.qualify.Add(name.name);
                                space.imports.Add(new ImportSpaceInfo(name.qualify));
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入命名空间名");
                        }
                        else if (lexical.anchor == KeyWords.NAMESPACE)
                        {
                            if (Lexical.TryExtractName(line, lexical.anchor.end, out var name, space.collector))
                            {
                                CheckEnd(line, name.name.end, space.collector);
                                var index = space;
                                foreach (var spaceName in name)
                                    index = new FileSpace(spaceName, index, index.space.GetChild(spaceName.ToString()), space.document, space.collector) { range = line };
                                index.space.attributes.AddRange(attributes);
                                attributes.Clear();
                                ParseSpace(index, reader, line.indent);
                                index.range &= reader.GetLastValidLine();
                                reader.Rollback();
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入命名空间名");
                        }
                        else
                        {
                            //todo 解析声明
                            ParseDeclaration(space, reader, line, attributes, annotations);
                        }
                    }
                    else if (lexical.IsReloadable)
                    {
                        //todo 解析重载函数
                    }
                    else space.collector.Add(lexical.anchor, ErrorLevel.Error, "应输入类型或命名空间");
                    annotations.Clear();
                    DiscardAttributes(attributes, space.collector);
                }
            }
            space.space.attributes.AddRange(attributes);
        }
    }
}
