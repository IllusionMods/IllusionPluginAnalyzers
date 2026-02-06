using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace DumpAllMembersFromAss
{
    /// <summary>
    /// Check assemblies of different KK versions for differences in ther types and type members
    /// output for use with the kk analyazer
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0 || !File.Exists(args[0]))
            {
                Console.WriteLine("File not found: " + args[0]);
                return;
            }

            var dir = Path.GetDirectoryName(args[0]);
            using var assDkn = AssemblyDefinition.ReadAssembly(args[0]);

            var allTypes = assDkn.MainModule.GetAllTypes().ToList();

            var typeFullnames = new List<string>();
            var memberFullnames = new List<string>();
            //var asDifferentConstants = new List<string>();
            foreach (var typeDefinition in allTypes)
            {
                if (typeDefinition.IsCompilerGenerated())
                    continue;

                var fullName = typeDefinition.FullName;
                typeFullnames.Add(fullName.Replace("/", "."));

                foreach (var typeDefinitionField in typeDefinition.GetAllMembers())
                {
                    if (typeDefinitionField.IsCompilerGenerated())
                        continue;

                    var name = typeDefinitionField.FullName;
                    memberFullnames.Add(fullName.Replace("/", ".") + "." + typeDefinitionField.Name);

                    //if (typeDefinitionField is FieldDefinition fieldDef)
                    //{
                    //    if (membersAs?.FirstOrDefault(x => x.FullName == name) is FieldDefinition asEnumVal && fieldDef.Constant?.ToString() != asEnumVal.Constant?.ToString())
                    //        asDifferentConstants.Add(fullName.Replace("/", ".") + "." + typeDefinitionField.Name);
                    //}
                }
            }

            File.WriteAllLines(dir + @"\allTypes.txt", typeFullnames);
            File.WriteAllLines(dir + @"\allMembers.txt", memberFullnames);

            Console.WriteLine($"Types:{typeFullnames.Count} Members:{memberFullnames.Count}");
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