using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Framework.Runtime;

namespace CustomLoader
{
    public class CscCompilation
    {
        private readonly IList<IMetadataReference> _references;
        private ILibraryExport _export;

        public CscCompilation(Project project,
                              IList<IMetadataReference> metadataReferences)
        {
            Project = project;
            MetadataReferences = metadataReferences;
        }

        public Project Project { get; private set; }

        public IList<IMetadataReference> MetadataReferences { get; private set; }

        public ILibraryExport GetExport()
        {
            if (_export == null)
            {
                var metadataReferences = new List<IMetadataReference>();
                var sourceReferences = new List<ISourceReference>();

                // Project reference
                metadataReferences.Add(new CscProjectReference(this));

                // Other references
                metadataReferences.AddRange(MetadataReferences);

                // Shared sources
                foreach (var sharedFile in Project.SharedFiles)
                {
                    sourceReferences.Add(new SourceFileReference(sharedFile));
                }

                _export = new LibraryExport(metadataReferences, sourceReferences);
            }

            return _export;
        }

        public void EmitAssemblyStream(Stream stream)
        {
            var tempDll = Path.Combine(Path.GetTempPath(), Project.Name + ".dll");

            // csc /out:foo.dll / target:library Program.cs
            var cscArgs = new StringBuilder()
                    .AppendFormat(@"/out:""{0}"" ", tempDll)
                    .Append("/target:library ")
                    .Append("/noconfig ")
                    .Append("/nostdlib ")
                    .Append(string.Join(" ", Project.SourceFiles.Select(s => "\"" + s + "\"")))
                    .Append(" ");

            var tempFiles = new List<string>
            {
                tempDll
            };

            // These are the metadata references being used by your project.
            // Everything in your project.json is resolved and normailzed here:
            // - Project references
            // - Package references are turned into the appropriate assemblies
            // - Assembly neutral references
            // Each IMetadaReference maps to an assembly
            foreach (var reference in MetadataReferences)
            {
                // Skip this project
                if (reference.Name == "CustomLoader")
                {
                    continue;
                }

                // NuGet references
                var fileReference = reference as IMetadataFileReference;
                if (fileReference != null)
                {
                    cscArgs.AppendFormat(@"/r:""{0}""", fileReference.Path)
                      .Append(" ");
                }

                // Assembly neutral references
                var embeddedReference = reference as IMetadataEmbeddedReference;
                if (embeddedReference != null)
                {
                    var tempEmbeddedPath = Path.Combine(Path.GetTempPath(), reference.Name + ".dll");

                    // Write the ANI to disk for csc
                    File.WriteAllBytes(tempEmbeddedPath, embeddedReference.Contents);

                    cscArgs.AppendFormat(@"/r:""{0}""", tempEmbeddedPath)
                      .Append(" ");

                    tempFiles.Add(tempEmbeddedPath);
                }

                var projectReference = reference as IMetadataProjectReference;
                if (projectReference != null)
                {
                    // You can write the reference assembly to the stream
                    // and add the reference to your compiler

                    var tempProjectDll = Path.Combine(Path.GetTempPath(), reference.Name + ".dll");

                    using (var fs = File.OpenWrite(tempProjectDll))
                    {
                        projectReference.WriteReferenceAssemblyStream(fs);
                    }

                    cscArgs.AppendFormat(@"/r:""{0}""", tempProjectDll)
                      .Append(" ");

                    tempFiles.Add(tempProjectDll);
                }
            }

            Console.WriteLine(cscArgs.ToString());

            var si = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\csc.exe"),
                Arguments = cscArgs.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(si);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return;
            }

            using (var fs = File.OpenRead(tempDll))
            {
                fs.CopyTo(stream);
            }

            // Nuke the temporary references on disk
            tempFiles.ForEach(File.Delete);
        }
    }
}