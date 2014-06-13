Custom project loaders
==============================

ASP.NET vNext supports a new project system that is powered by project.json. The default loader is the roslyn compiler which is built
into the runtime. This means that if you put a project.json in a folder with C# source, at runtime it will become an assembly
compiled on the fly with the built in roslyn compiler.

This sample shows how you can specify the loader for a particular project with a few examples:
- A custom loader that resolves references but does nothing with then and returns null
- An F# loader that will use fsc to compile F# sources and load the resulting assembly

Mind not blown yet?

- The F# loader is a project reference in the same solution, written in C# source code. The F# loader is being compiled with
roslyn into an assembly that is then executed to produce an assembly using the f# compiler (fsc.exe), which is then returned to
the system.
