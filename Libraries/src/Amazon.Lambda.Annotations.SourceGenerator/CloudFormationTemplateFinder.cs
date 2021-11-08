using System;
using System.IO;
using System.Linq;
using Amazon.Lambda.Annotations.SourceGenerator.Diagnostics;
using Amazon.Lambda.Annotations.SourceGenerator.FileIO;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Amazon.Lambda.Annotations.SourceGenerator
{
    public class CloudFormationTemplateFinder
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly IDiagnosticReporter _diagnosticReporter;

        public CloudFormationTemplateFinder(IFileManager fileManager, IDirectoryManager directoryManager, IDiagnosticReporter diagnosticReporter)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
            _diagnosticReporter = diagnosticReporter;
        }

        public string DetermineProjectRootDirectory(string sourceFilePath)
        {
            if (!_fileManager.Exists(sourceFilePath))
                return string.Empty;

            var directoryPath = _directoryManager.GetDirectoryName(sourceFilePath);
            while (!string.IsNullOrEmpty(directoryPath))
            {
                if (_directoryManager.GetFiles(directoryPath, "*.csproj").Length == 1)
                    return directoryPath;
                directoryPath = _directoryManager.GetDirectoryName(directoryPath);
            }

            return string.Empty;
        }

        public string FindCloudFormationTemplate(string projectRootDirectory)
        {
            if (!_directoryManager.Exists(projectRootDirectory))
            {
                _diagnosticReporter.Report(Diagnostic.Create(CloudFormationTemplateDescriptors.ProjectRootNotFound, Location.None));
                throw new DirectoryNotFoundException(string.Format(CloudFormationTemplateDescriptors.ProjectRootNotFound.MessageFormat.ToString(), projectRootDirectory));
            }

            var templateAbsolutePath = string.Empty;

            var defaultConfigFile = _directoryManager.GetFiles(projectRootDirectory, "aws-lambda-tools-defaults.json", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (_fileManager.Exists(defaultConfigFile))
                // the templateAbsolutePath will be empty if the template property is not found in the default config file
                templateAbsolutePath = GetTemplatePathFromDefaultConfigFile(defaultConfigFile);

            // if the default config file does not exist or if the template property is not found in the default config file
            // set the template path inside the project root directory.
            if (string.IsNullOrEmpty(templateAbsolutePath))
                templateAbsolutePath = Path.Combine(projectRootDirectory, "serverless.template");

            if (!_fileManager.Exists(templateAbsolutePath))
                _fileManager.Create(templateAbsolutePath).Close();

            return templateAbsolutePath;
        }

        private string GetTemplatePathFromDefaultConfigFile(string defaultConfigFile)
        {
            JToken rootToken;
            try
            {
                rootToken = JObject.Parse(_fileManager.ReadAllText(defaultConfigFile));
            }
            catch (Exception)
            {
                return string.Empty;
            }

            var templateRelativePath = rootToken["template"]?.ToObject<string>();

            if (string.IsNullOrEmpty(templateRelativePath))
                return string.Empty;

            var templateAbsolutePath = Path.Combine(_directoryManager.GetDirectoryName(defaultConfigFile), templateRelativePath);
            return templateAbsolutePath;
        }
    }
}