using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Framework.Runtime;
using NuGet;
using System.Collections.Generic;

namespace Loader.FSharp
{
    public class FSharpAssemblyLoader : IAssemblyLoader
    {
        private readonly IProjectResolver _projectResolver;
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryExportProvider _exportProvider;
        private readonly IAssemblyLoaderEngine _loaderEngine;

        public FSharpAssemblyLoader(IProjectResolver projectResolver,
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

            string outputFile = Path.Combine(project.ProjectDirectory, "bin", VersionUtility.GetShortFrameworkName(_environment.TargetFramework), assemblyName + ".dll");
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            var args = new StringBuilder();
            args.Append("--noframework ")
                .AppendFormat(@"--out:""{0}""", outputFile)
                .Append(" ")
                .Append("--target:library ")
                .Append(string.Join(" ", project.SourceFiles.Select(s => "\"" + s + "\"")))
                .Append(" ");

            var tempFiles = new List<string>();

            foreach (var reference in export.MetadataReferences)
            {
                // References to nuget packges on disk
                var fileRef = reference as IMetadataFileReference;
                if (fileRef != null)
                {
                    args.AppendFormat(@"-r:""{0}""", fileRef.Path)
                        .Append(" ");
                }

                // References to other projects
                var projectRef = reference as IMetadataProjectReference;
                if (projectRef != null)
                {
                    var tempReferenceFile = Path.Combine(Path.GetTempPath(), reference.Name + ".dll");

                    // Write the assembly reference to disk
                    using (var fs = File.OpenWrite(tempReferenceFile))
                    {
                        projectRef.WriteReferenceAssemblyStream(fs);
                    }

                    args.AppendFormat(@"-r:""{0}""", tempReferenceFile)
                        .Append(" ");

                    tempFiles.Add(tempReferenceFile);
                }

                // Assembly neutral references
                var embeddedRef = reference as IMetadataEmbeddedReference;
                if (embeddedRef != null)
                {
                    // Write the assembly reference to disk
                    var tempEmbeddedReferenceFile = Path.Combine(Path.GetTempPath(), reference.Name + ".dll");

                    File.WriteAllBytes(tempEmbeddedReferenceFile, embeddedRef.Contents);

                    args.AppendFormat(@"-r:""{0}""", tempEmbeddedReferenceFile)
                        .Append(" ");

                    tempFiles.Add(tempEmbeddedReferenceFile);
                }
            }

            Console.WriteLine(args.ToString());

            var si = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\Fsc.exe",
                Arguments = args.ToString(),
#if NET45
                UseShellExecute = false,
#endif
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(si);
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return null;
            }

            // Nuke the temporary references on disk
            tempFiles.ForEach(File.Delete);
            
            return _loaderEngine.LoadFile(outputFile);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

    }
}
