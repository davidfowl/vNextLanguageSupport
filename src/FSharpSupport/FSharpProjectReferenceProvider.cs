using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace FSharpSupport
{
    public class FSharpProjectReferenceProvider : IProjectReferenceProvider
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

            // Represents the project reference
            return new FSharpProjectReference(project, targetFramework, configuration, incomingReferences);
        }
    }
}
