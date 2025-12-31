// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of MiHoYoTools.

// SRTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SRTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with MiHoYoTools.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>

using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using MiHoYoTools.Depend;
using MiHoYoTools;
using MiHoYoTools.Core;

namespace MiHoYoTools.Depend
{
    class GetUpdate
    {
        private static readonly GetGithubLatest _getGithubLatest = new GetGithubLatest();
        private static readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();

        public static async Task<UpdateResult> GetSRToolsUpdate()
        {
            UpdateResult result = await OnGetUpdateLatestReleaseInfo("SRTools");
            return result;
        }

        public static async Task<UpdateResult> GetDependUpdate()
        {
            UpdateResult result = await OnGetUpdateLatestReleaseInfo("SRToolsHelper", "Depend");
            return result;
        }

        private static async Task<UpdateResult> OnGetUpdateLatestReleaseInfo(string PkgName, string Mode = null)
        {
            string currentVersion = AppInfoHelper.GetVersionString();
            Version currentVersionParsed = AppInfoHelper.GetVersion();
            try
            {
                Logging.Write("Getting Update Info...", 0);
                (string Name, string Version, string DownloadUrl, string Changelog) latestReleaseInfo;
                if (Mode == "Depend")
                {
                    latestReleaseInfo = await _getGithubLatest.GetLatestDependReleaseInfoAsync(
                        HelperSources.HelperRepoOwner,
                        HelperSources.HelperRepoName,
                        PkgName);
                }
                else
                {
                    latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
                    switch (AppDataController.GetUpdateService())
                    {
                        case 0:
                            Logging.Write("UpdateService:Github", 0);
                            latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", PkgName);
                            break;
                        case 2:
                            Logging.Write("UpdateService:JSG-DS", 0);
                            latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg." + PkgName);
                            break;
                        default:
                            Logging.Write($"Invalid update service value: {AppDataController.GetUpdateService()}", 0);
                            throw new InvalidOperationException($"Invalid update service value: {AppDataController.GetUpdateService()}");
                    }
                }

                Logging.Write("Software Name:" + latestReleaseInfo.Name, 0);
                Logging.Write("Newer Version:" + latestReleaseInfo.Version, 0);

                if (Mode == "Depend")
                {
                    string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string iniPath = Path.Combine(userDocumentsFolderPath, "JSG-LLC", "SRTools", "Depends", PkgName, "SRToolsHelperVersion.ini");
                    string exePath = Path.Combine(userDocumentsFolderPath, "JSG-LLC", "SRTools", "Depends", PkgName, "SRToolsHelper.exe");

                    Version installedVersionParsed;

                    if (File.Exists(iniPath))
                    {
                        string[] iniLines = await File.ReadAllLinesAsync(iniPath);
                        if (iniLines != null)
                        {
                            string versionString = iniLines[0].Trim();
                            installedVersionParsed = new Version(versionString);
                        }
                        else
                        {
                            installedVersionParsed = new Version("0.0.0.0");
                        }
                    }
                    else if (File.Exists(exePath))
                    {
                        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(exePath);
                        installedVersionParsed = new Version(fileInfo.FileVersion);
                    }
                    else
                    {
                        installedVersionParsed = new Version("0.0.0.0");
                    }

                    Version latestVersionParsed = new Version(latestReleaseInfo.Version);
                    if (latestVersionParsed > installedVersionParsed)
                    {
                        App.IsSRToolsHelperRequireUpdate = true;
                        return new UpdateResult(1, latestReleaseInfo.Version, latestReleaseInfo.Changelog);
                    }
                    App.IsSRToolsHelperRequireUpdate = false;
                    return new UpdateResult(0, installedVersionParsed.ToString(), string.Empty);
                }
                else
                {
                    Version latestVersionParsed = new Version(latestReleaseInfo.Version);

                    if (latestVersionParsed > currentVersionParsed)
                    {
                        App.IsSRToolsRequireUpdate = true;
                        return new UpdateResult(1, latestReleaseInfo.Version, latestReleaseInfo.Changelog);
                    }
                    App.IsSRToolsRequireUpdate = false;
                    return new UpdateResult(0, currentVersion, string.Empty);
                }
            }
            catch (Exception)
            {
                return new UpdateResult(2, string.Empty, string.Empty);
            }
        }
    }

    public class UpdateResult
    {
        public int Status { get; set; } // 0: 无更新, 1: 有更新, 2: 错误
        public string Version { get; set; } // 版本号
        public string Changelog { get; set; } // 更新日志

        public UpdateResult(int status, string version, string changelog)
        {
            Status = status;
            Version = version;
            Changelog = changelog;
        }
    }
}



