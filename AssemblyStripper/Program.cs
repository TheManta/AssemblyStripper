using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.IO;

namespace AssemblyStripper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: stripper.exe <inputDirectory>");
                return;
            }

            string inputDirectory = args[0];
            if (!Directory.Exists(inputDirectory))
            {
                Console.WriteLine($"Couldn't find directory: {inputDirectory}");
                return;
            }

            Console.WriteLine($"Stripping assemblies in: {inputDirectory}");

            foreach (string filePath in Directory.GetFiles(inputDirectory))
            {
                StripAssembly(filePath);
            }

            Console.WriteLine("Finished stripping assemblies.");
        }

        /// <summary>
        /// Strips the IL from the given assembly, leaving only a reference assembly with no executable code.
        /// </summary>
        private static void StripAssembly(string filePath)
        {
            Console.WriteLine($"Stripping Assembly: {Path.GetFileName(filePath)}");

            ModuleContext context = ModuleDef.CreateModuleContext();
            ModuleDefMD module = ModuleDefMD.Load(filePath, context);

            foreach (TypeDef type in module.Types)
            {
                StripType(type);
            }

            // HACK: What if we need to reference embedded resources like localized strings?  Can we empty just the resource CONTENTS?
            module.Resources?.Clear();

            string outPath = Path.Combine(Path.GetDirectoryName(filePath), "Stripped", Path.GetFileName(filePath));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            if (module.IsILOnly)
            {
                module.Write(outPath, new ModuleWriterOptions(module)
                {
                    ShareMethodBodies = true
                });
            }
            else
            {
                // Mixed Mode Assembly
                module.NativeWrite(outPath, new NativeModuleWriterOptions(module, optimizeImageSize: true)
                {
                    ShareMethodBodies = true
                });
            }
        }

        /// <summary>
        /// Strips the given Type's method bodies and nested Types.
        /// </summary>
        private static void StripType(TypeDef type)
        {
            CilBody throwNullBody = new CilBody
            {
                Instructions = { new Instruction(OpCodes.Ldnull), new Instruction(OpCodes.Throw) }
            };


            foreach (MethodDef method in type.Methods)
            {
                if (method.HasBody)
                {
                    method.Body = throwNullBody;
                }
            }

            foreach (TypeDef nestedType in type.NestedTypes)
            {
                StripType(nestedType);
            }
        }
    }
}
