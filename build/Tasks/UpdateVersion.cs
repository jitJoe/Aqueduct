using System;
using System.IO;
using System.Text.RegularExpressions;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Frosting;

[TaskName("UpdateVersion")]
public sealed class UpdateVersion : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        var major = context.HasArgument("major");
        var minor = context.HasArgument("minor");
        var patch = context.HasArgument("patch");

        if (!major && !minor && !patch)
        {
            throw new Exception("Must pass --major, --minor or --patch when updating version");
        }

        var clientCsProj = File.ReadAllText($"{Environment.CurrentDirectory}/Aqueduct.Client/Aqueduct.Client.csproj");
        var serverCsProj = File.ReadAllText($"{Environment.CurrentDirectory}/Aqueduct.Server/Aqueduct.Server.csproj");
        var sharedCsProj = File.ReadAllText($"{Environment.CurrentDirectory}/Aqueduct.Shared/Aqueduct.Shared.csproj");
        
        var versionRegex = new Regex("<PackageVersion>([0-9]+\\.[0-9]+\\.[0-9]+)<\\/PackageVersion>");
        var currentVersion = versionRegex.Match(clientCsProj).Groups[1].Value;
        var currentMajor = int.Parse(currentVersion.Split(".")[0]);
        var currentMinor = int.Parse(currentVersion.Split(".")[1]);
        var currentPatch = int.Parse(currentVersion.Split(".")[2]);

        if (major)
        {
            currentMajor += 1;
            currentMinor = 0;
            currentPatch = 0;
        }

        if (minor)
        {
            currentMinor += 1;
            currentPatch = 0;
        }

        if (patch)
        {
            currentPatch += 1;
        }

        var newVersion = $"{currentMajor}.{currentMinor}.{currentPatch}";
        var newVersionXml = $"<PackageVersion>{newVersion}</PackageVersion>";
        
        context.Information($"Current Version: {currentVersion}, New Version: {newVersion}");

        File.WriteAllText($"{Environment.CurrentDirectory}/Aqueduct.Client/Aqueduct.Client.csproj", 
            versionRegex.Replace(clientCsProj, newVersionXml));
        File.WriteAllText($"{Environment.CurrentDirectory}/Aqueduct.Server/Aqueduct.Server.csproj", 
            versionRegex.Replace(serverCsProj, newVersionXml));
        File.WriteAllText($"{Environment.CurrentDirectory}/Aqueduct.Shared/Aqueduct.Shared.csproj", 
            versionRegex.Replace(sharedCsProj, newVersionXml));
    }
}