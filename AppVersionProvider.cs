using System.Reflection;

namespace WWCduDcsBiosBridge.Services;

public static class AppVersionProvider
{
    public static string GetAppVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var ver = string.IsNullOrWhiteSpace(info) ? asm.GetName().Version?.ToString() : info;
        if (string.IsNullOrWhiteSpace(ver)) return "0.0.0";
        var clean = ver.Split('+', '-')[0].Trim(); // Trim build metadata / prerelease
        return clean.TrimStart('v', 'V');
    }
}