using System.Threading.Tasks;
using CliFx;

namespace HelloWorld
{
    public static class Program
    {
        public static async Task<int> Main() =>
            await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .UseExecutableName("s2t")
                .Build()
                .RunAsync();
    }
}