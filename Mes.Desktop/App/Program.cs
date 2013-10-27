namespace GitHub
{
    using GitHub.Helpers;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class Program : IProgram
    {
        public Program()
        {
            Assembly assembly = typeof(Program).Assembly;
            this.AssemblyName = assembly.GetName();
            this.ExecutingAssemblyDirectory = Path.GetDirectoryName(assembly.Location);
        }

        public System.Reflection.AssemblyName AssemblyName { get; private set; }

        public string ExecutingAssemblyDirectory { get; private set; }
    }
}

