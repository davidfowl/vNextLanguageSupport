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
            IEnumerable<IMetadataReference> incomingReferences, 
            IEnumerable<ISourceReference> incomingSourceReferences, 
            IList<IMetadataReference> outgoingReferences)
        {
            // Represents the project reference
            return new FSharpProjectReference(project, targetFramework, configuration, incomingReferences);
        }
    }
}
