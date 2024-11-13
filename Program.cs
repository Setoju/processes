using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace ProcessTreeManager
{
    static class Program
    {
        static void Main()
        {
            while (true)
            {
                Console.WriteLine("\nUsage:");
                Console.WriteLine("show all processes");
                Console.WriteLine("show process tree <PID>");
                Console.WriteLine("kill process tree <PID>");
                Console.WriteLine("exit\n");

                string? input = Console.ReadLine()?.Trim();

                if (input == null)
                {
                    Console.WriteLine("Bad input!");
                    continue;
                }

                if (input.Equals("show all processes", StringComparison.OrdinalIgnoreCase))
                {
                    DisplayAllProcesses();
                }
                else if (input.StartsWith("show process tree", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length == 4 && int.TryParse(parts[3], out int pid))
                    {
                        DisplayProcessTree(pid);
                    }
                    else
                    {
                        Console.WriteLine("Wrong format! Usage: show process tree <PID>");
                    }
                }
                else if (input.StartsWith("kill process tree", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length == 4 && int.TryParse(parts[3], out int pid))
                    {
                        KillProcessTree(pid);
                    }
                    else
                    {
                        Console.WriteLine("Wrong format! Usage: kill process tree <PID>");
                    }
                }
                else if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                else
                {
                    Console.WriteLine("Unknown command!");
                }
            }
        }

        static void DisplayAllProcesses()
        {
            ManagementObjectSearcher searcher = new("SELECT ProcessId, ParentProcessId, Name FROM Win32_Process");

            foreach (ManagementBaseObject obj in searcher.Get())
            {
                int processId = Convert.ToInt32(obj["ProcessId"]);
                int parentProcessId = Convert.ToInt32(obj["ParentProcessId"]);
                string name = Convert.ToString(obj["Name"]) ?? "Unknown";

                Console.WriteLine($"Process ID: {processId}, Parent Process ID: {parentProcessId}, Name: {name}");
            }
        }

        static void DisplayProcessTree(int pid)
        {
            var processTree = BuildProcessTree();

            Console.WriteLine($"\nProcesses with Parent Process ID {pid}:");

            bool found = false;
            foreach (var kvp in processTree)
            {
                if (kvp.Key == pid)
                {
                    foreach (int childPid in kvp.Value)
                    {
                        Console.WriteLine($"Process ID: {childPid}");
                        found = true;
                    }
                }
            }

            if (!found)
            {
                Console.WriteLine($"No processes found with Parent Process ID {pid}.");
            }
        }

        static Dictionary<int, List<int>> BuildProcessTree()
        {
            Dictionary<int, List<int>> processTree = new();

            ManagementObjectSearcher searcher = new("SELECT ProcessId, ParentProcessId FROM Win32_Process");

            foreach (ManagementObject obj in searcher.Get())
            {
                int processId = Convert.ToInt32(obj["ProcessId"]);
                int parentProcessId = Convert.ToInt32(obj["ParentProcessId"]);

                if (!processTree.ContainsKey(parentProcessId))
                {
                    processTree[parentProcessId] = new List<int>();
                }

                processTree[parentProcessId].Add(processId);
            }

            return processTree;
        }

        static void KillProcessTree(int pid)
        {
            var processTree = BuildProcessTree();

            if (!processTree.ContainsKey(pid))
            {
                Console.WriteLine($"Process with PID {pid} wasn't found.");
                return;
            }

            Console.WriteLine($"\nStopping processes with Parent Process ID {pid}:");

            // Рекурсивно зупиняємо всі дочірні процеси
            KillChildProcesses(pid, processTree);

            // Тепер зупиняємо батьківський процес
            KillProcess(pid);
        }

        static void KillChildProcesses(int pid, Dictionary<int, List<int>> processTree)
        {
            if (processTree.ContainsKey(pid))
            {
                foreach (int childPid in processTree[pid])
                {
                    // Рекурсивно зупиняємо дочірні процеси
                    KillChildProcesses(childPid, processTree);
                    KillProcess(childPid); // Зупиняємо поточний процес
                }
            }
        }

        static void KillProcess(int pid)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/F /PID {pid}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                }

                Console.WriteLine($"Process with PID {pid} is stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while stopping with PID {pid}: {ex.Message}");
            }
        }
    }
}