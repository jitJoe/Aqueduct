using Cake.Core;
using Cake.Frosting;

public class Program : IFrostingStartup
{
    public static int Main(string[] args)
    {
        var host = new CakeHostBuilder()
            .WithArguments(args)
            .UseStartup<Program>()
            .Build();
        
        return host.Run();
    }

    public void Configure(ICakeServices services)
    {
        services.UseContext<Context>();
        services.UseLifetime<Lifetime>();
        services.UseWorkingDirectory("..");
    }
}