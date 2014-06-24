using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace CustomLoader
{
    public class CscLoader : IAssemblyLoader, ILibraryExportProvider
    {
        private readonly IProjectResolver _projectResolver;
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryExportProvider _exportProvider;
        private readonly IAssemblyLoaderEngine _loaderEngine;

        public CscLoader(IProjectResolver projectResolver,
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

            var compilation = CreateCompilation(project, _environment.TargetFramework);

            var tempFile = Path.Combine(Path.GetTempPath(), "CustomLoader", ".loaded", project.Name + ".dll");
            Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

            using (var fs = File.OpenWrite(tempFile))
            {
                compilation.EmitAssemblyStream(fs);
            }

            return _loaderEngine.LoadFile(tempFile);
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
            var compilation = CreateCompilation(project, targetFramework);

            return compilation.GetExport();
        }

        private CscCompilation CreateCompilation(Project project, FrameworkName targetFramework)
        {
            // This call could be cached

            var projectExportProvider = new ProjectExportProvider(_projectResolver);
            FrameworkName effectiveTargetFramework;
            var export = projectExportProvider.GetProjectExport(_exportProvider, project.Name, targetFramework, out effectiveTargetFramework);

            return new CscCompilation(project, export.MetadataReferences);
        }
    }
}
