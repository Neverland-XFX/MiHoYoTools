// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of MiHoYoTools.Modules.Zenless.

// ZenlessTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ZenlessTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with MiHoYoTools.Modules.Zenless.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static MiHoYoTools.App;

namespace MiHoYoTools.Modules.Zenless.Depend
{
    class ProcessRun
    {
        private static string ResolveHelperExePath()
        {
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dependsRoot = Path.Combine(docsPath, "JSG-LLC", "ZenlessTools", "Depends");
            string helperDir = Path.Combine(dependsRoot, "ZenlessToolsHelper");
            string helperExePath = Path.Combine(helperDir, "ZenlessToolsHelper.exe");

            if (File.Exists(helperExePath))
            {
                return helperExePath;
            }

            string zipPath = Path.Combine(dependsRoot, "ZenlessToolsHelper.zip");
            if (File.Exists(zipPath))
            {
                try
                {
                    Directory.CreateDirectory(helperDir);
                    ZipFile.ExtractToDirectory(zipPath, helperDir, true);
                }
                catch (Exception ex)
                {
                    Logging.Write($"Failed to extract helper: {ex.Message}", 3, "ZenlessToolsHelper");
                }
            }

            return helperExePath;
        }

        public static async Task<string> ZenlessToolsHelperAsync(string args)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string helperPath = ResolveHelperExePath();
                    if (!File.Exists(helperPath))
                    {
                        throw new FileNotFoundException("ZenlessToolsHelper.exe not found.", helperPath);
                    }

                    using (Process process = new Process())
                    {
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true; // 捕获标准错误输出
                        process.StartInfo.FileName = helperPath;
                        process.StartInfo.Arguments = args;

                        Logging.Write($"Starting process: {process.StartInfo.FileName} with arguments: {args}", 0, "ZenlessToolsHelper");

                        process.Start();


                        // 同时读取标准输出和标准错误
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Logging.Write($"Error: {error}", 3, "ZenlessToolsHelper");
                        }

                        Logging.Write(output.Trim(), 3, "ZenlessToolsHelper");
                        return output.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write($"Exception in ZenlessToolsHelperAsync: {ex.Message}", 3, "ZenlessToolsHelper");
                    throw;
                }
            });
        }

        public static void StopZenlessToolsHelperProcess()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("ZenlessToolsHelper"))
                {
                    process.Kill();
                }
                NotificationManager.RaiseNotification("ZenlessToolsHelper", "已停止依赖运行", InfoBarSeverity.Warning, true, 3);
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("错误", "停止ZenlessToolsHelper失败" + ex.ToString(), InfoBarSeverity.Error, true, 3);
            }
        }

        public static void StopSRProcess()
        {
            foreach (var process in Process.GetProcessesByName("ZenlessZoneZero"))
            {
                process.Kill();
            }
        }

        public async static Task RestartApp()
        {
            Logging.Write("Restart ZenlessTools Requested", 2);
            var processId = Process.GetCurrentProcess().Id;
            var fileName = Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo info = new ProcessStartInfo(fileName)
            {
                UseShellExecute = true,
            };
            Process.Start(info);
            Process.GetProcessById(processId).Kill();
        }

        public async static Task RequestAdminAndRestart()
        {
            Logging.Write("Restart ZenlessTools Requested", 2);
            var processId = Process.GetCurrentProcess().Id;
            var fileName = Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo info = new ProcessStartInfo(fileName)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(info);
            Process.GetProcessById(processId).Kill();
        }

        public static bool IsRunAsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}


