using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace FSharpSupport
{
    public class FSharpProjectExportProvider : IProjectExportProvider
    {
        public ILibraryExport GetProjectExport(Project project, FrameworkName targetFramework, string configuration, ILibraryExport projectExport)
        {
            var metadataReferences = new List<IMetadataReference>();
            var sourceReferences = new List<ISourceReference>();

            // Represents the project reference
            metadataReferences.Add(new FSharpProjectReference(project, targetFramework, configuration, projectExport));

            // Other references
            metadataReferences.AddRange(projectExport.MetadataReferences);

            // Shared sources
            foreach (var sharedFile in project.SharedFiles)
            {
                sourceReferences.Add(new SourceFileReference(sharedFile));
            }

            return new LibraryExport(metadataReferences, sourceReferences);
        }
    }
}
