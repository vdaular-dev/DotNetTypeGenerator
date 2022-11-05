using System.Text;

namespace DotNetTypeGenerator;

public static class StringBuilderExtensions
{
    public static void UsingNamespace(this StringBuilder sb, string ns)
        => sb.AppendLine($"using {ns};");

    public static void WriteLine(this StringBuilder sb, string text)
        => sb.AppendLine(text);

    public static void Write(this StringBuilder sb, string text)
        => sb.Append(text);

    public static void Namespace(this StringBuilder sb, string ns)
        => sb.AppendLine($"namespace {ns};");

    public static void StartBlock(this StringBuilder sb)
        => sb.AppendLine("{");

    public static void FinishBlock(this StringBuilder sb)
        => sb.AppendLine("}");

    public static void StartClass(this StringBuilder sb, string className)
    {
        sb.AppendLine($"public class {className}");
        sb.StartBlock();
    }

    public static void UsingBlock(this StringBuilder sb, string declaration, Action<StringBuilder> inner)
    {
        sb.Write($"using ({declaration})");
        sb.StartBlock();
        inner(sb);
        sb.FinishBlock();
    }
}
