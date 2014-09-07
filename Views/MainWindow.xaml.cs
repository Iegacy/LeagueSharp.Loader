﻿#region

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LeagueSharp.Loader.Class;
using LeagueSharp.Loader.Data;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

#endregion

/*
    Copyright (C) 2014 LeagueSharp

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace LeagueSharp.Loader.Views
{
    public partial class MainWindow : MetroWindow
    {
        public Config Config;

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Utility.CreateFileFromResource("config.xml", "LeagueSharp.Loader.Resources.config.xml");
            Config = ((Config)Utility.MapXmlFileToClass(typeof(Config), "config.xml"));

            Browser.Visibility = Visibility.Hidden;
            DataContext = this;
            GameSettingsDataGrid.ItemsSource = Config.Settings.GameSettings;
            LogsDataGrid.ItemsSource = Logs.MainLog.Items;
            InstalledAssembliesDataGrid.ItemsSource = Config.InstalledAssemblies;

            //Try to login with the saved credentials.
            if (!Auth.Login(Config.Username, Config.Password).Item1)
            {
                ShowLoginDialog();
            }
            else
            {
                OnLogin(Config.Username);
            }
        }

        private async void ShowLoginDialog()
        {
            MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;
            var result =
                await
                    this.ShowLoginAsync(
                        "LeagueSharp", "Sign in",
                        new LoginDialogSettings { ColorScheme = MetroDialogOptions.ColorScheme });

            var loginResult = new Tuple<bool, string>(false, "Cancel button pressed");
            if (result != null)
            {
                var hash = Auth.Hash(result.Password);

                Config.Username = result.Username;
                Config.Password = hash;

                loginResult = Auth.Login(result.Username, hash);
            }

            if (result != null && loginResult.Item1)
            {
                OnLogin(result.Username);
            }
            else
            {
                ShowAfterLoginDialog(string.Format("Failed to login: {0}", loginResult.Item2), true);
                Utility.Log(
                    LogStatus.Error, "Login",
                    string.Format(
                        "Failed to sign in as {0}: {1}", (result != null ? result.Username : "null"), loginResult.Item2),
                    Logs.MainLog);
            }
        }

        private async void ShowAfterLoginDialog(string message, bool showLoginDialog)
        {
            await this.ShowMessageAsync("Login", message);
            if (showLoginDialog)
            {
                ShowLoginDialog();
            }
        }

        private void OnLogin(string username)
        {
            Utility.Log(LogStatus.Ok, "Login", string.Format("Succesfully signed in as {0}", username), Logs.MainLog);
            Browser.Visibility = Visibility.Visible;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new InstallerWindow { Owner = this };
            window.ShowDialog();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Utility.MapClassToXmlFile(typeof(Config), Config, "config.xml");
        }

        private void InstalledAssembliesDataGrid_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            if (dataGrid != null)
            {
                if (dataGrid.SelectedItems.Count == 0)
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void CompileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var selectedAssemblyData in InstalledAssembliesDataGrid.SelectedItems)
            {
                var selectedAssembly = (LeagueSharpAssembly)selectedAssemblyData;
                selectedAssembly.Compile();
            }
        }

        private void UpdateMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var selectedAssemblyData in InstalledAssembliesDataGrid.SelectedItems)
            {
                var selectedAssembly = (LeagueSharpAssembly)selectedAssemblyData;
                selectedAssembly.Update();
            }
        }

        private void UpdateAndCompileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateMenuItem_OnClick(null, null);
            CompileMenuItem_OnClick(null, null);
        }

        private void RemoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }
            var remove = InstalledAssembliesDataGrid.SelectedItems.Cast<LeagueSharpAssembly>().ToList();

            foreach (var ee in remove)
            {
                Config.InstalledAssemblies.Remove(ee);
            }
        }

        private void GithubItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }
            var selectedAssembly = (LeagueSharpAssembly)InstalledAssembliesDataGrid.SelectedItems[0];
            if (selectedAssembly.SvnUrl != "")
            {
                System.Diagnostics.Process.Start(selectedAssembly.SvnUrl);
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Config.Username = "";
            Config.Password = "";
            MainWindow_OnClosing(null, null);
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
    }
}