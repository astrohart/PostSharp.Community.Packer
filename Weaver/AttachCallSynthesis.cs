using PostSharp.Extensibility;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.Packer.Weaver
{
    public class AttachCallSynthesis
    {
        public void SynthesizeCallToAttach(PackerAttribute config,
            Project project, AssemblyLoaderInfo assemblyLoaderInfo)
        {
            if (config == null) return;
            if (project == null) return;
            if (assemblyLoaderInfo == null) return;

            var initialized = FindInitializeCalls(project, assemblyLoaderInfo);

            if (config.LoadAtModuleInit)
                AddModuleInitializerCall(project, assemblyLoaderInfo);
            else if (!initialized)
                Message.Write(
                    project.Module.Assembly.GetSystemAssembly(),
                    SeverityType.Warning, "PACK01",
                    "The add-in was not initialized. Make sure LoadAtModuleInit=true or call PackerUtility.Initialize()."
                );
        }

        private static void AddModuleInitializerCall(Project project,
            AssemblyLoaderInfo assemblyLoaderInfo)
        {
            if (project == null) return;
            if (assemblyLoaderInfo == null) return;

            var task = project.GetTask<AspectInfrastructureTask>();
            task.TypeInitializationManager.RegisterClient(
                new CallAttachModuleInitializer(
                    assemblyLoaderInfo.AttachMethod
                ), TypeInitializationClientScopes.Module
            );
        }

        private static bool FindInitializeCalls(Project project,
            AssemblyLoaderInfo assemblyLoaderInfo)
        {
            if (project == null) return false;
            if (assemblyLoaderInfo == null) return false;

            var packerUtilityType =
                (INamedType)project.Module.FindType(typeof(PackerUtility));
            var packerUtilityInitialize = project
                                          .Module.FindMethod(
                                              packerUtilityType, "Initialize"
                                          )
                                          .GetMethodDefinition();
            var weaver = new Sdk.CodeWeaver.Weaver(project);
            var replacingAdvice =
                new ReplacePackerUtilityInitializeAdvice(
                    assemblyLoaderInfo.AttachMethod
                );
            weaver.AddMethodLevelAdvice(
                replacingAdvice, null, JoinPointKinds.InsteadOfCall,
                new[] { packerUtilityInitialize }
            );
            weaver.Weave();
            return replacingAdvice.ReplacedAtLeastOneCall;
        }
    }
}