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
using System.IO;
using static MiHoYoTools.App;

namespace MiHoYoTools.Depend
{
    public class CreateShortcut
    {
        public static async void CreateDesktopShortcut()
        {
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MiHoYoTools.url");
            using (StreamWriter writer = new StreamWriter(shortcutPath))
            {
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=mihoyotools:///starrail/startgame");
            }
            NotificationManager.RaiseNotification("Shortcut created", "MiHoYoTools shortcut has been created on your desktop.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, true, 2);
        }
    }
}



