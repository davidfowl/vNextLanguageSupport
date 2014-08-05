using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Framework.Runtime;
using Microsoft.FSharp.Compiler.SimpleSourceCodeServices;

namespace FSharpSupport
{
    /// <summary>
    /// Summary description for FSharpProjectReference
    /// </summary>
    public class FSharpProjectReference : IMetadataProjectReference
    {
        private readonly Project _project;
        private readonly FrameworkName _targetFramework;
        private readonly string _configuration;
        private readonly IEnumerable<IMetadataReference> _metadataReferences;

        public FSharpProjectReference(Project project,
                                      FrameworkName targetFramework,
                                      string configuration,
                                      IEnumerable<IMetadataReference> metadataReferences)
        {
            _project = project;
            _targetFramework = targetFramework;
            _configuration = configuration;
            _metadataReferences = metadataReferences;
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
            return _project.SourceFiles.Select(p => (ISourceReference)new SourceFileReference(p))
                                       .ToList();
        }

        public IDiagnosticResult Emit(string outputPath, bool emitPdb, bool emitDocFile, bool emitExe = false)
        {
            var tempBasePath = Path.Combine(outputPath, _project.Name, "obj");
            var outputDll = Path.Combine(outputPath, _project.Name + (emitExe ? ".exe" : ".dll"));

            // csc /out:foo.dll / target:library Program.cs
            var fscArgBuilder = new List<string>();
            fscArgBuilder.Add("fsc.exe");
            fscArgBuilder.Add("--noframework");
            fscArgBuilder.Add("--nologo");
            fscArgBuilder.Add("--out:" + outputDll);
            fscArgBuilder.Add("--target:" + (emitExe ? "exe" : "library"));

            Directory.CreateDirectory(tempBasePath);

            if (emitPdb)
            {
                var pdb = Path.Combine(outputPath, _project.Name + ".pdb");

                fscArgBuilder.Add("--debug");
                fscArgBuilder.Add("--optimize-");
                fscArgBuilder.Add("--tailcalls-");
                fscArgBuilder.Add("--pdb:" + pdb);
            }

            if (emitDocFile)
            {
                var doc = Path.Combine(outputPath, _project.Name + ".xml");

                fscArgBuilder.Add("--doc:" + doc);
            }

            // F# cares about order so assume that the files were listed in order
            fscArgBuilder.AddRange(_project.SourceFiles);

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
                if (reference.Name == typeof(FSharpProjectReference).Assembly.GetName().Name)
                {
                    continue;
                }

                // NuGet references
                var fileReference = reference as IMetadataFileReference;
                if (fileReference != null)
                {
                    fscArgBuilder.Add(@"-r:" + fileReference.Path);
                }

                // Assembly neutral references
                var embeddedReference = reference as IMetadataEmbeddedReference;
                if (embeddedReference != null)
                {
                    var tempEmbeddedPath = Path.Combine(tempBasePath, reference.Name + ".dll");

                    // Write the ANI to disk for csc
                    File.WriteAllBytes(tempEmbeddedPath, embeddedReference.Contents);

                    fscArgBuilder.Add("-r:" + tempEmbeddedPath);

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

                    fscArgBuilder.Add(@"-r:" + tempProjectDll);

                    tempFiles.Add(tempProjectDll);
                }
            }

            // For debugging
            // Console.WriteLine(fscArgs.ToString());

            var scs = new SimpleSourceCodeServices();
            var result = scs.Compile(fscArgBuilder.ToArray());

            var warnings = result.Item1.Where(i => i.Severity.IsWarning).Select(i => i.ToString()).ToArray();
            var errors = result.Item1.Where(i => i.Severity.IsError).Select(i => i.ToString()).ToArray();

            if (result.Item2 != 0)
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