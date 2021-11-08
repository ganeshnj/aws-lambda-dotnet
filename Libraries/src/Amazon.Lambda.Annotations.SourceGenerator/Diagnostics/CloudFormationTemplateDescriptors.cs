using Microsoft.CodeAnalysis;

namespace Amazon.Lambda.Annotations.SourceGenerator.Diagnostics
{
    public static class CloudFormationTemplateDescriptors
    {
        public static readonly DiagnosticDescriptor ProjectRootNotFound = new DiagnosticDescriptor(id: "AWSLambda0101",
            title: "Project root not found",
            messageFormat: "Failed to find project root directory. {0} does not exist",
            category: "CloudFormationTemplate",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}