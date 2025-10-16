//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.Cli;

/// <summary>
/// Entry point for the Blood Thinner Tracker CLI tool.
/// Provides command-line interface for medication and INR tracking.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point for the CLI application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static async Task<int> Main(string[] args)
    {
        await Console.Out.WriteLineAsync("Blood Thinner Tracker CLI - Coming Soon!");
        await Console.Out.WriteLineAsync("This tool will help you track medications and INR tests from the command line.");
        return 0;
    }
}
