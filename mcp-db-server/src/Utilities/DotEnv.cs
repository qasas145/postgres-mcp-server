using System;
using System.IO;

public static class DotEnv
{
    public static void Load(string filePath = ".env")
    {
        // Try to find .env file in current directory or parent directories
        var currentDir = Directory.GetCurrentDirectory();
        var envFile = Path.Combine(currentDir, filePath);

        // If not found, try parent directory (for when running from bin/Debug/net8.0)
        if (!File.Exists(envFile))
        {
            var parentDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;
            if (parentDir != null)
            {
                envFile = Path.Combine(parentDir, filePath);
            }
        }

        if (!File.Exists(envFile))
        {
            Console.WriteLine($"⚠️  Warning: {filePath} file not found. Using system environment variables.");
            return;
        }

        Console.WriteLine($"✓ Loading environment variables from: {envFile}");

        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var parts = trimmedLine.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Remove quotes if present
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value[1..^1];
            else if (value.StartsWith("'") && value.EndsWith("'"))
                value = value[1..^1];

            // Only set if not already set in system environment
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        Console.WriteLine("✓ Environment variables loaded successfully\n");
    }
}
