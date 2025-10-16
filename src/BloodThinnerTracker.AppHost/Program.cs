//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.AppHost
{
    using BloodThinnerTracker.ServiceDefaults;

    /// <summary>
    /// Entry point for the Blood Thinner Tracker Application Host.
    /// Provides basic service orchestration and configuration management.
    /// NOTE: Full Aspire integration will be added in future development phases.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Main entry point for the application host.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code (0 for success).</returns>
        public static async Task<int> Main(string[] args)
        {
            await Console.Out.WriteLineAsync($"🩸 {ServiceDefaults.ApplicationName} v{ServiceDefaults.Version} - Application Host");
            await Console.Out.WriteLineAsync("========================================");
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync("🚀 Services Available:");
            await Console.Out.WriteLineAsync("  • BloodThinnerTracker.Api      - REST API (Port: 5000)");
            await Console.Out.WriteLineAsync("  • BloodThinnerTracker.Web      - Blazor Web UI (Port: 5001)");
            await Console.Out.WriteLineAsync("  • BloodThinnerTracker.Cli      - Command Line Tool");
            await Console.Out.WriteLineAsync("  • BloodThinnerTracker.Mcp      - MCP Server (Port: 5002)");
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync("📋 Configuration:");
            await Console.Out.WriteLineAsync($"  • Database: {ServiceDefaults.DefaultConnectionString}");
            await Console.Out.WriteLineAsync($"  • CORS Policy: {ServiceDefaults.DefaultCorsPolicy}");
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync("⚠️  MEDICAL DISCLAIMER:");
            await Console.Out.WriteLineAsync("   This software is for informational purposes only.");
            await Console.Out.WriteLineAsync("   Always consult with qualified healthcare professionals for medical decisions.");
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync("🔧 Next Development Phase: Full .NET Aspire integration");
            await Console.Out.WriteLineAsync("   Run individual services with: dotnet run --project <service>");

            return 0;
        }
    }
}
