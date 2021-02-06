using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.NuGet.Push;
using Cake.Core;
using Cake.Frosting;

[TaskName("PublishNuGet")]
public sealed class PublishNuGet : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        var apiKey = context.Environment.GetEnvironmentVariable("NUGET_KEY");
        var nugetLocation = context.Environment.GetEnvironmentVariable("NUGET_LOCATION");

        if (apiKey == null)
        {
            throw new Exception("You must set an environment variable of NUGET_KEY before running this task.");
        }
        
        if (nugetLocation == null)
        {
            throw new Exception("You must set an environment variable of NUGET_LOCATION before running this task.");
        }

        var clientCsProj = File.ReadAllText($"{Environment.CurrentDirectory}/Aqueduct.Client/Aqueduct.Client.csproj");
        var versionRegex = new Regex("<PackageVersion>([0-9]+\\.[0-9]+\\.[0-9]+)<\\/PackageVersion>");
        var currentVersion = versionRegex.Match(clientCsProj).Groups[1].Value;        
        
        Publish(context, apiKey, currentVersion, nugetLocation, "Client");
        Publish(context, apiKey, currentVersion, nugetLocation, "Server");
        Publish(context, apiKey, currentVersion, nugetLocation, "Shared");
    }

    private void Publish(Context context, string apiKey, string version, string nugetLocation, string project)
    {
        var csProj = $"{Environment.CurrentDirectory}/Aqueduct.{project}/Aqueduct.{project}.csproj";
        var dotnetCoreMsBuildSettings = new DotNetCoreMSBuildSettings();

        context.DotNetCorePack(csProj, new DotNetCorePackSettings {
            Configuration = "Release",
            OutputDirectory = $"{Environment.CurrentDirectory}/Aqueduct.{project}/package",
            MSBuildSettings = dotnetCoreMsBuildSettings
        });
        
        context.NuGetPush($"{Environment.CurrentDirectory}/Aqueduct.{project}/package/Aqueduct.{project}.{version}.nupkg", 
            new NuGetPushSettings {
                Source = "https://www.nuget.org/api/v2/package",
                ApiKey = apiKey,
                ToolPath = nugetLocation
        });
    }
}