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
            IEnumerable<IMetadataReference> incomingReferences, 
            IEnumerable<ISourceReference> incomingSourceReferences, 
            IList<IMetadataReference> outgoingReferences)
        {
            return new CscProjectReference(project, targetFramework, configuration, incomingReferences, incomingSourceReferences);
        }
    }
}
