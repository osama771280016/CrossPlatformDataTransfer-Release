using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Startup;

namespace CrossPlatformDataTransfer.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Setup Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register all project services using our Bootstrapper
        services.AddAllProjectServices();

        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var tcpServer = serviceProvider.GetRequiredService<ITcpAgentServer>();

        logger.LogInformation("=== CrossPlatformDataTransfer CLI - TCP Server Mode ===");

        // Setup Event Handlers
        tcpServer.AgentConnected += (s, e) =>
        {
            Console.WriteLine($"\n[EVENT] Agent Connected!");
            Console.WriteLine($"Device ID: {e.DeviceId}");
            Console.WriteLine($"Model: {e.Model}");
            Console.WriteLine($"Version: {e.Version}");
            Console.Write("\nCLI> ");
        };

        tcpServer.MessageReceived += (s, e) =>
        {
            if (e.Command != "PONG") // Don't spam heartbeat
            {
                Console.WriteLine($"\n[MESSAGE] Received Command: {e.Command}");
                if (!string.IsNullOrEmpty(e.JsonHeader))
                    Console.WriteLine($"Header: {e.JsonHeader}");
                if (e.Payload != null)
                    Console.WriteLine($"Payload Size: {e.Payload.Length} bytes");
                Console.Write("\nCLI> ");
            }
        };

        tcpServer.AgentDisconnected += (s, e) =>
        {
            Console.WriteLine("\n[EVENT] Agent Disconnected.");
            Console.Write("\nCLI> ");
        };

        // Start Server on port 4711
        const int port = 4711;
        try 
        {
            await tcpServer.StartAsync(port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start server. Is ADB installed?");
            return;
        }

        Console.WriteLine("\nCommands available:");
        Console.WriteLine("- 'get-info': Request system info from the agent");
        Console.WriteLine("- 'ping': Send a ping to the agent");
        Console.WriteLine("- 'exit': Stop the server and exit");

        while (true)
        {
            Console.Write("CLI> ");
            var input = Console.ReadLine()?.ToLower().Trim();

            if (input == "exit") break;

            if (input == "get-info")
            {
                await tcpServer.SendCommandAsync("GET_SYS_INFO");
            }
            else if (input == "ping")
            {
                await tcpServer.SendCommandAsync("PING");
            }
            else if (!string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Unknown command.");
            }
        }

        await tcpServer.StopAsync();
        logger.LogInformation("Exiting...");
    }
}
