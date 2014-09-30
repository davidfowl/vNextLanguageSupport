using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace CscSupport
{
    public class CscProjectReferenceProvider : IProjectReferenceProvider
    {
        public IMetadataProjectReference GetProjectReference(
            Project project, 
            ILibraryKey libraryKey,
            Func<ILibraryExport> referenceResolver,
            IList<IMetadataReference> outgoingReferences)
        {
            var export = referenceResolver();
            var incomingReferences = export.MetadataReferences;
            var incomingSourceReferences = export.SourceReferences;

            return new CscProjectReference(project, libraryKey.TargetFramework, libraryKey.Configuration, incomingReferences, incomingSourceReferences);
        }
    }
}
