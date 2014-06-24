using System;
using System.IO;
using Microsoft.Framework.Runtime;

namespace CustomLoader
{
    public class CscProjectReference : IMetadataProjectReference
    {
        private readonly CscCompilation _compilation;

        public CscProjectReference(CscCompilation compilation)
        {
            _compilation = compilation;
        }


        public string Name
        {
            get
            {
                return _compilation.Project.Name;
            }
        }

        public string ProjectPath
        {
            get
            {
                return _compilation.Project.ProjectFilePath;
            }
        }

        public void WriteReferenceAssemblyStream(Stream stream)
        {
            _compilation.EmitAssemblyStream(stream);
        }
    }
}