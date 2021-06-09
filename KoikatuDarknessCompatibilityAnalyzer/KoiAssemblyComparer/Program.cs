using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace KoiAssemblyComparer
{
    /// <summary>
    /// Check assemblies of different KK versions for differences in ther types and type members
    /// output for use with the kk analyazer
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            var dir = @"E:\a\";

            var pathDkn = dir + @"dkn\Assembly-CSharp.dll";
            var pathAs = dir + @"as\Assembly-CSharp.dll";
            var pathKkp = dir + @"kkp\Assembly-CSharp.dll";

            using var assDkn = AssemblyDefinition.ReadAssembly(pathDkn);
            using var assAs = AssemblyDefinition.ReadAssembly(pathAs);
            using var assKkp = AssemblyDefinition.ReadAssembly(pathKkp);

            var typesDkn = assDkn.MainModule.GetAllTypes().ToList();
            var typesAs = assAs.MainModule.GetAllTypes().ToList();
            var typesKkp = assKkp.MainModule.GetAllTypes().ToList();

            var asMissingTypes = new List<string>();
            var kkpMissingTypes = new List<string>();

            var asMissingMembers = new List<string>();
            var kkpMissingMembers = new List<string>();
            var sameAs = new List<string>();
            var sameKkp = new List<string>();
            foreach (var typeDefinition in typesDkn)
            {
                if (typeDefinition.IsCompilerGenerated())
                    continue;

                var fullName = typeDefinition.FullName;
                var typedefAs = typesAs.Find(x => x.FullName == fullName);
                if (typedefAs == null)
                    asMissingTypes.Add(fullName.Replace("/", "."));
                else
                    sameAs.Add(fullName);
                var typedefKkp = typesKkp.Find(x => x.FullName == fullName);
                if (typedefKkp == null)
                    kkpMissingTypes.Add(fullName.Replace("/", "."));
                else
                    sameKkp.Add(fullName);

                var membersAs = typedefAs?.GetAllMembers().ToList();
                var membersKkp = typedefKkp?.GetAllMembers().ToList();

                foreach (var typeDefinitionField in typeDefinition.GetAllMembers())
                {
                    if (typeDefinitionField.IsCompilerGenerated())
                        continue;

                    var name = typeDefinitionField.FullName;
                    if (membersAs?.Any(x => x.FullName == name) != true)
                        asMissingMembers.Add(fullName.Replace("/", ".") + "." + typeDefinitionField.Name);
                    if (membersKkp?.Any(x => x.FullName == name) != true)
                        kkpMissingMembers.Add(fullName.Replace("/", ".") + "." + typeDefinitionField.Name);
                }
            }

            File.WriteAllLines(dir + @"asMissingTypes.txt", asMissingTypes);
            File.WriteAllLines(dir + @"kkpMissingTypes.txt", kkpMissingTypes);
            File.WriteAllLines(dir + @"asMissingMembers.txt", asMissingMembers);
            File.WriteAllLines(dir + @"kkpMissingMembers.txt", kkpMissingMembers);

            Console.WriteLine(asMissingMembers.Count);
        }
    }

    internal static class Extensions
    {
        public static IEnumerable<IMemberDefinition> GetAllMembers(this TypeDefinition tdef)
        {
            return Enumerable.Empty<IMemberDefinition>()
                .Concat(tdef.Fields)
                .Concat(tdef.Methods)
                .Concat(tdef.Properties)
                .Concat(tdef.Events);
        }

        public static bool IsCompilerGenerated(this IMemberDefinition typeDefinition)
        {
            return typeDefinition.FullName.Contains("<") ||
                   typeDefinition.CustomAttributes.Any(x => x.AttributeType.Name == "CompilerGeneratedAttribute");
        }
    }
}