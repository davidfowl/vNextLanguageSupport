using System;
using System.Collections.Generic;
using System.IO;
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
                throw new NotImplementedException();
            }
        }

        public string ProjectPath
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IProjectBuildResult EmitAssembly(string outputPath)
        {
            throw new NotImplementedException();
        }

        public IProjectBuildResult EmitAssembly(Stream assemblyStream, Stream pdbStream)
        {
            throw new NotImplementedException();
        }

        public void EmitReferenceAssembly(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IProjectBuildResult GetDiagnostics()
        {
            throw new NotImplementedException();
        }

        public IList<ISourceReference> GetSources()
        {
            throw new NotImplementedException();
        }
    }
}