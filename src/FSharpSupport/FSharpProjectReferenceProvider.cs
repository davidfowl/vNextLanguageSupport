using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace FSharpSupport
{
    public class FSharpProjectReferenceProvider : IProjectReferenceProvider
    {
        public IMetadataProjectReference GetProjectReference(Project project, FrameworkName targetFramework, string configuration, ILibraryExport projectExport)
        {
            // Represents the project reference
            return new FSharpProjectReference(project, targetFramework, configuration, projectExport);
        }
    }
}
