using Microsoft.CodeAnalysis;

namespace Amazon.Lambda.Annotations.SourceGenerator.Diagnostics
{
    public static class CSharpGeneratorDescriptors
    {
        public static readonly DiagnosticDescriptor MultipleStartupNotAllowed = new DiagnosticDescriptor(id: "AWSLambda0001",
            title: "Multiple LambdaStartup classes not allowed",
            messageFormat: "Multiple LambdaStartup classes are not allowed in Lambda AWSProjectType",
            category: "AWSLambdaCSharpGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}