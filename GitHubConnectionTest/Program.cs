using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("GitHub Connection Test");
        Console.WriteLine("======================");
        Console.WriteLine();

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Setup logging
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Run test
        var tester = new GitHubAppTester(configuration, logger);
        var success = await tester.TestConnectionAsync();

        Console.WriteLine();
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ All tests passed!");
            Console.ResetColor();
            Environment.Exit(0);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Tests failed!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Verify App ID and Installation ID in appsettings.json");
            Console.WriteLine("  2. Check PEM file path and content");
            Console.WriteLine("  3. Verify GitHub App is installed on the repository");
            Environment.Exit(1);
        }
    }
}
