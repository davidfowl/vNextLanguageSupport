using System;
using Microsoft.Framework.Runtime;

namespace MySampleApplication
{
    /// <summary>
    /// Summary description for Class
    /// </summary>
    public class Program
    {
        private readonly IApplicationEnvironment _env;

        public Program(IApplicationEnvironment env)
        {
            _env = env;
        }

        public void Main(string[] args)
        {
            int x;
            var c = new Classlibrary1.Class1();
            Console.WriteLine(c);

            Console.WriteLine("Using an assembly neutral interface the app name is {0}", _env.ApplicationName);
        }
    }
}