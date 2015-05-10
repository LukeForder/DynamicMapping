using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicMapping
{
    public class MapBuilder
    {
        public void Generate(params Type[] typesToMap)
        {
            Guid uuid = Guid.NewGuid();

            // this needs to be unique across all loaded assemblies
            string assemblyName = string.Format("mapping_x{0}", uuid.ToString("N"));
            AssemblyName asmName = new AssemblyName(assemblyName);

            string fileName = string.Format("{0}.dll", assemblyName);

            AppDomain currentDomain = Thread.GetDomain();
            AssemblyBuilder builder = currentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mbuilder = builder.DefineDynamicModule("MapModule", fileName, true);

            foreach (var type in typesToMap)
            {
                mbuilder.AddMapForType(type);
            }
            
            builder.Save(fileName);

        }
        
        
    }
}
