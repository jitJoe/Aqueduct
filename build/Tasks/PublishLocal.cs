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
using Cake.Core;
using Cake.Frosting;

[TaskName("PublishLocal")]
public sealed class PublishLocal : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        var localNuGetRepository = context.Environment.GetEnvironmentVariable("LOCAL_NUGET_REPOSITORY");

        if (localNuGetRepository == null)
        {
            throw new Exception("You must set an environment variable of LOCAL_NUGET_REPOSITORY before running this task.");
        }

        Publish(context, localNuGetRepository, "Client");
        Publish(context, localNuGetRepository, "Server");
        Publish(context, localNuGetRepository, "Shared");
    }

    private void Publish(Context context, string localNuGetRepository, string project)
    {
        var csProj = $"{Environment.CurrentDirectory}/Aqueduct.{project}/Aqueduct.{project}.csproj";
        var dotnetCoreMsBuildSettings = new DotNetCoreMSBuildSettings();

        context.DotNetCorePack(csProj, new DotNetCorePackSettings {
            Configuration = "Release",
            OutputDirectory = localNuGetRepository,
            MSBuildSettings = dotnetCoreMsBuildSettings
        });

    }
}