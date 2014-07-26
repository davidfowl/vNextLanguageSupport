using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace FSharpSupport
{
    /// <summary>
    /// Summary description for FSharpProjectReference
    /// </summary>
    public class FSharpProjectReference : IMetadataProjectReference
    {
        private readonly string _configuration;
        private readonly Project _project;
        private readonly ILibraryExport _projectExport;
        private readonly FrameworkName _targetFramework;

        public FSharpProjectReference(Project project, FrameworkName targetFramework, string configuration, ILibraryExport projectExport)
        {
            _project = project;
            _targetFramework = targetFramework;
            _configuration = configuration;
            _projectExport = projectExport;
        }

        public string Name
        {
            get
            {
                return _project.Name;
            }
        }

        public string ProjectPath
        {
            get
            {
                return _project.ProjectFilePath;
            }
        }

        public Assembly Load(IAssemblyLoaderEngine loaderEngine)
        {
            throw new NotImplementedException();
        }

        public IProjectBuildResult EmitAssembly(string outputPath)
        {
            throw new NotImplementedException();
        }

        public void EmitReferenceAssembly(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IProjectBuildResult GetDiagnostics()
        {
            return new ProjectBuildResult(success: false, warnings: Enumerable.Empty<string>(), errors: Enumerable.Empty<string>());
        }

        public IList<ISourceReference> GetSources()
        {
            return new List<ISourceReference>();
        }
    }
}