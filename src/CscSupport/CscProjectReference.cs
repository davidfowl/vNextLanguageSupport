using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Framework.Runtime;

namespace CscSupport
{
    public class CscProjectReference : IMetadataProjectReference
    {
        private readonly FrameworkName _targetFramework;
        private readonly Project _project;
        private readonly string _configuration;
        private readonly IEnumerable<IMetadataReference> _metadataReferences;
        private readonly IEnumerable<ISourceReference> _sourceReferences;

        public CscProjectReference(Project project,
                                   FrameworkName targetFramework,
                                   string configuration,
                                   IEnumerable<IMetadataReference> metadataReferences,
                                   IEnumerable<ISourceReference> sourceReferences)
        {
            _project = project;
            _targetFramework = targetFramework;
            _configuration = configuration;
            _metadataReferences = metadataReferences;
            _sourceReferences = sourceReferences;
        }

        public string Name
        {
            get
            {
                return _project.Name;
            }
        }

        public string ProjectPath
        {
            get
            {
                return _project.ProjectFilePath;
            }
        }

        public Assembly Load(IAssemblyLoaderEngine loaderEngine)
        {
            string outputDir = Path.Combine(Path.GetTempPath(), "dynamic-assemblies");

            var result = Emit(outputDir, emitPdb: true, emitDocFile: false);

            if (!result.Success)
            {
                throw new CompilationException(result.Errors.ToList());
            }

            var assemblyPath = Path.Combine(outputDir, _project.Name + ".dll");

            return loaderEngine.LoadFile(assemblyPath);
        }

        public IDiagnosticResult EmitAssembly(string outputPath)
        {
            return Emit(outputPath, emitPdb: true, emitDocFile: true);
        }

        public void EmitReferenceAssembly(Stream stream)
        {
            string outputDir = Path.Combine(Path.GetTempPath(), "reference-assembly-" + Guid.NewGuid().ToString());

            try
            {
                var result = Emit(outputDir, emitPdb: false, emitDocFile: false);

                if (!result.Success)
                {
                    return;
                }

                using (var fs = File.OpenRead(Path.Combine(outputDir, _project.Name + ".dll")))
                {
                    fs.CopyTo(stream);
                }
            }
            finally
            {
                Directory.Delete(outputDir, true);
            }
        }

        public IDiagnosticResult GetDiagnostics()
        {
            string outputDir = Path.Combine(Path.GetTempPath(), "diagnostics-" + Guid.NewGuid().ToString());

            try
            {
                return Emit(outputDir, emitPdb: false, emitDocFile: false);
            }
            finally
            {
                Directory.Delete(outputDir, true);
            }
        }

        public IList<ISourceReference> GetSources()
        {
            return _project.SourceFiles.Select(p => (ISourceReference)new SourceFileReference(p)).ToList();
        }

        public IDiagnosticResult Emit(string outputPath, bool emitPdb, bool emitDocFile)
        {
            var tempBasePath = Path.Combine(outputPath, _project.Name, "obj");
            var outputDll = Path.Combine(outputPath, _project.Name + ".dll");

            // csc /out:foo.dll / target:library Program.cs
            var cscArgBuilder = new StringBuilder()
                    .AppendFormat(@"/out:""{0}"" ", outputDll)
                    .Append("/target:library ")
                    .Append("/noconfig ")
                    .Append("/nostdlib ")
                    .Append("/nologo ");

            Directory.CreateDirectory(tempBasePath);

            if (emitPdb)
            {
                var pdb = Path.Combine(outputPath, _project.Name + ".pdb");

                cscArgBuilder = cscArgBuilder
                    .Append("/debug ")
                    .AppendFormat(@"/pdb:""{0}"" ", pdb);
            }

            if (emitDocFile)
            {
                var doc = Path.Combine(outputPath, _project.Name + ".xml");

                cscArgBuilder = cscArgBuilder
                    .AppendFormat(@"/doc:""{0}"" ", doc);
            }

            var cscArgs = cscArgBuilder
                    .Append(string.Join(" ", _project.SourceFiles.Select(s => "\"" + s + "\"")))
                    .Append(" ");

            var tempFiles = new List<string>();

            // These are the metadata references being used by your project.
            // Everything in your project.json is resolved and normailzed here:
            // - Project references
            // - Package references are turned into the appropriate assemblies
            // - Assembly neutral references
            // Each IMetadaReference maps to an assembly
            foreach (var reference in _metadataReferences)
            {
                // Skip this project
                if (reference.Name == typeof(CscProjectReference).Assembly.GetName().Name)
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
                    var tempEmbeddedPath = Path.Combine(tempBasePath, reference.Name + ".dll");

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

                    var tempProjectDll = Path.Combine(tempBasePath, reference.Name + ".dll");

                    using (var fs = File.OpenWrite(tempProjectDll))
                    {
                        projectReference.EmitReferenceAssembly(fs);
                    }

                    cscArgs.AppendFormat(@"/r:""{0}""", tempProjectDll)
                      .Append(" ");

                    tempFiles.Add(tempProjectDll);
                }
            }

            // For debugging
            // Console.WriteLine(cscArgs.ToString());

            var cscPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\csc.exe");
            var useShellExecute = true;

            if (File.Exists(cscPath))
            {
                useShellExecute = true;
            }
            else
            {
                cscPath = "mcs";
            }

            var si = new ProcessStartInfo
            {
                FileName = cscPath,
                Arguments = cscArgs.ToString(),
                UseShellExecute = !useShellExecute,
                CreateNoWindow = useShellExecute,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(si);
            process.WaitForExit();

            var errors = new List<string>();
            var warnings = new List<string>();

            string line = null;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                if (line.Contains(" warning "))
                {
                    warnings.Add(line);
                }
                else
                {
                    errors.Add(line);
                }
            }

            if (process.ExitCode != 0)
            {
                return new DiagnosticResult(success: false, warnings: warnings, errors: errors);
            }

            // Nuke the temporary references on disk
            tempFiles.ForEach(File.Delete);

            Directory.Delete(tempBasePath);

            return new DiagnosticResult(success: true, warnings: warnings, errors: errors);
        }
    }
}