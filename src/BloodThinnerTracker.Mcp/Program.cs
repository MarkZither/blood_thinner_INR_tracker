//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.Mcp;

/// <summary>
/// Entry point for the Blood Thinner Tracker MCP (Model Context Protocol) Server.
/// Provides AI assistant integration for medication and INR tracking.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point for the MCP server.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static async Task<int> Main(string[] args)
    {
        await Console.Out.WriteLineAsync("Blood Thinner Tracker MCP Server - Coming Soon!");
        await Console.Out.WriteLineAsync("This server will provide AI assistant integration capabilities.");
        return 0;
    }
}
