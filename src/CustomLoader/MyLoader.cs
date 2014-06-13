using System;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace CustomLoader
{
    public class MyLoader : IAssemblyLoader, ILibraryExportProvider
    {
        private readonly IProjectResolver _projectResolver;
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryExportProvider _exportProvider;
        private readonly IAssemblyLoaderEngine _loaderEngine;

        public MyLoader(IProjectResolver projectResolver,
                        IApplicationEnvironment environment,
                        ILibraryExportProvider exportProvider,
                        IAssemblyLoaderEngine loaderEngine)
        {
            _projectResolver = projectResolver;
            _environment = environment;
            _exportProvider = exportProvider;
            _loaderEngine = loaderEngine;
        }

        public Assembly Load(string assemblyName)
        {
            Project project;
            if (!_projectResolver.TryResolveProject(assemblyName, out project))
            {
                return null;
            }

            var projectExportProvider = new ProjectExportProvider(_projectResolver);
            FrameworkName effectiveTargetFramework;
            var export = projectExportProvider.GetProjectExport(_exportProvider, assemblyName, _environment.TargetFramework, out effectiveTargetFramework);


            // These are the metadata references being used by your project.
            // Everything in your project.json is resolved and normailzed here:
            // - Project references
            // - Package references are turned into the appropriate assemblies
            // Each IMetadaReference maps to an assembly
            foreach (var reference in export.MetadataReferences)
            {
                var fileReference = reference as IMetadataFileReference;
                if (fileReference != null)
                {
                    Console.WriteLine(fileReference.Path);
                }

                // Right now, project references are exposed as IRoslynMetadataReference
            }

            return null;
        }

        public ILibraryExport GetLibraryExport(string name, FrameworkName targetFramework)
        {
            Project project;
            if (!_projectResolver.TryResolveProject(name, out project))
            {
                return null;
            }

            // If somebody wants to compile against the project, you need to provide
            // a library export. This is the closure of metadata references for your
            // dependencies and yourself.

            // If you don't export any APIs then returning null is fine
            return null;
        }
    }
}
