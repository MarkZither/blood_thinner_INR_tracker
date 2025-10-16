//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.AppHost;

/// <summary>
/// Entry point for the Blood Thinner Tracker Aspire AppHost.
/// Orchestrates all services in the distributed application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point for the application host.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Task representing the application lifetime.</returns>
    public static async Task<int> Main(string[] args)
    {
        await Console.Out.WriteLineAsync("Blood Thinner Tracker AppHost - Coming Soon!");
        await Console.Out.WriteLineAsync("Aspire orchestration will be configured in the next development phase.");
        return 0;
    }
}
