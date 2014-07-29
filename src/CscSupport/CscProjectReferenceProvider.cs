using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace CscSupport
{
    public class CscProjectReferenceProvider : IProjectReferenceProvider
    {
        public IMetadataProjectReference GetProjectReference(Project project, FrameworkName targetFramework, string configuration, ILibraryExport projectExport)
        {
            return new CscProjectReference(project, targetFramework, configuration, projectExport);
        }
    }
}
