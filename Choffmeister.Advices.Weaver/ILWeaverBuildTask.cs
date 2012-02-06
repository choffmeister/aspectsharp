using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Choffmeister.Advices.Weaver
{
    public class ILWeaverBuildTask : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        public string AssemblyDirectories { get; set; }

        public bool OptimizedCode { get; set; }

        public override bool Execute()
        {
            try
            {
                ILWeaver weaver = new ILWeaver();
                AssemblyDefinition weavedAssembly = null;

                string[] assemblyDirectories = (this.AssemblyDirectories ?? string.Empty).Split(new char[] { ',' });

                using (FileStream input = File.Open(this.AssemblyPath, FileMode.Open))
                {
                    weavedAssembly = weaver.Weave(input, assemblyDirectories, this.OptimizedCode);
                }

                using (FileStream output = File.Open(this.AssemblyPath, FileMode.Create))
                {
                    weavedAssembly.Write(output);
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(ex.Message + "\n\n" + ex.StackTrace);
            }

            return !this.Log.HasLoggedErrors;
        }
    }
}