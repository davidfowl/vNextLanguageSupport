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
            FrameworkName targetFramework, 
            string configuration,
            Func<ILibraryExport> referenceResolver,
            IList<IMetadataReference> outgoingReferences)
        {
            var export = referenceResolver();
            var incomingReferences = export.MetadataReferences;
            var incomingSourceReferences = export.SourceReferences;

            return new CscProjectReference(project, targetFramework, configuration, incomingReferences, incomingSourceReferences);
        }
    }
}
