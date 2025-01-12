﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;
using System.Windows.Media.Animation;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.FirstLaunch;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.Play;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Core.Pages.Common;
using BedrockLauncher.Pages.Community;

namespace BedrockLauncher
{
    //TODO: (Later On) Community Content / Personal Donations Section

    public partial class MainWindow : Window
    {
        private GameTabs MainPage = new GameTabs();
        private SettingsTabs settingsScreenPage = new SettingsTabs();
        private NewsScreenTabs newsScreenPage = new NewsScreenTabs(ViewModels.LauncherModel.Updater);
        private CommunityPage communityPage = new CommunityPage();
        private Dungeons.Pages.GameTabs dungeonsPage = new Dungeons.Pages.GameTabs();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) Init();
        }

        private void Init()
        {

            Panel.SetZIndex(OverlayFrame, 0);
            Panel.SetZIndex(ErrorFrame, 1);
            Panel.SetZIndex(UpdateButton, 2);

            MainPage.skinsPage.InitFrameEvents(ErrorFrame);
            MainPage.skinsPage.InitFrameEvents(OverlayFrame);

            UpdateButton.ClickBase.Click += LauncherModel.Updater.UpdateButton_Click;
            LauncherModel.Default.InitKeyboardFocus(MainFrame);

            bool isFirstLaunch = Properties.LauncherSettings.Default.CurrentProfile == "" || 
                Properties.LauncherSettings.Default.IsFirstLaunch || 
                LauncherModel.Default.Config.profiles.Count() == 0;

            ButtonManager_Base(BedrockEditionButton.Name);
            if (isFirstLaunch) LauncherModel.Default.SetOverlayFrame_Strict(new WelcomePage());
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ViewModels.LauncherModel.Default.AttemptClose(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LauncherModel.Default.GameManager.Cancel();
        }

        #region Navigation

        private async void Navigate(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;

            if (!animate)
            {
                await MainWindowFrame.Dispatcher.InvokeAsync(() => MainWindowFrame.Navigate(content));
                return;
            }
            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Down;
            else direction = ExpandDirection.Up;

            await Task.Run(() => BedrockLauncher.Core.Components.PageAnimator.FrameSwipe(MainWindowFrame, content, direction));
        }

        public async void ResetButtonManager(string buttonName)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() { 
                // main window
                CommunityButton.Button,
                NewsButton.Button,
                BedrockEditionButton.Button,
                DungeonsButton.Button,
                JavaEditionButton.Button,
                SettingsButton.Button,
            };

                foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

                if (toggleButtons.Exists(x => x.Name == buttonName))
                {
                    toggleButtons.Where(x => x.Name == buttonName).FirstOrDefault().IsChecked = true;
                }
            });

        }
        public async void ButtonManager(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var toggleButton = sender as ToggleButton;
                string name = toggleButton.Name;
                Task.Run(() => ButtonManager_Base(name));
            });
        }
        public async void ButtonManager_Base(string senderName)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == BedrockEditionButton.Name) NavigateToMainPage();
                else if (senderName == DungeonsButton.Name) NavigateToDungeons();
                else if (senderName == NewsButton.Name) NavigateToNewsPage();
                else if (senderName == JavaEditionButton.Name) NavigateToJavaLauncher();
                else if (senderName == ExternalLauncherButton.Name) NavigateToExternalLauncher();
                else if (senderName == CommunityButton.Name) NavigateToCommunityScreen();
                else if (senderName == SettingsButton.Name) NavigateToSettings();
            });

        }

        public async void NavigateToNewsPage()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LauncherModel.Default.UpdatePageIndex(0);
                NewsButton.Button.IsChecked = true;
                Task.Run(() => Navigate(newsScreenPage));
            });

        }
        public async void NavigateToMainPage()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LauncherModel.Default.UpdatePageIndex(1);
                BedrockEditionButton.Button.IsChecked = true;
                Task.Run(() => Navigate(MainPage));
            });

        }
        public async void NavigateToDungeons()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LauncherModel.Default.UpdatePageIndex(2);
                DungeonsButton.Button.IsChecked = true;
                Task.Run(() => Navigate(dungeonsPage));
            });

        }
        public async void NavigateToCommunityScreen()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LauncherModel.Default.UpdatePageIndex(3);
                CommunityButton.Button.IsChecked = true;
                Task.Run(() => Navigate(communityPage));
            });
        }
        public async void NavigateToSettings()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LauncherModel.Default.UpdatePageIndex(4);
                SettingsButton.Button.IsChecked = true;
                Task.Run(() => Navigate(settingsScreenPage));
            });

        }
        public async void NavigateToJavaLauncher()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                Action action = new Action(() =>
                {
                    string JavaPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher.lnk";

                    try
                    {
                        // Trying to find and open Java launcher shortcut
                        Process.Start(JavaPath);
                        if (Properties.LauncherSettings.Default.CloseLauncherOnSwitch) Application.Current.MainWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("CantFindJavaLauncher: {0}", JavaPath));
                        Debug.WriteLine(ex);
                        MainPage.NavigateToPlayScreen();
                        ErrorScreenShow.errormsg("Error_CantFindJavaLauncher_Title", "Error_CantFindJavaLauncher", ex);
                    }
                });

                NavigateToOtherLauncher(action);
            });
        }
        public async void NavigateToExternalLauncher()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                Action action = new Action(() =>
                {
                    string LauncherPath = Properties.LauncherSettings.Default.ExternalLauncherPath;
                    string Arguments = Properties.LauncherSettings.Default.ExternalLauncherArguments;
                    try
                    {
                        Process.Start(LauncherPath, Arguments);
                        if (Properties.LauncherSettings.Default.CloseLauncherOnSwitch) Application.Current.MainWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("CantFindExternalLauncher:\n\nPath: {0}\nArguments: {1}", LauncherPath, Arguments));
                        Debug.WriteLine(ex);
                        MainPage.NavigateToPlayScreen();
                        ErrorScreenShow.errormsg("Error_CantFindExternalLauncher_Title", "Error_CantFindExternalLauncher", ex);
                    }
                });

                NavigateToOtherLauncher(action);
            });

        }
        public async void NavigateToOtherLauncher(Action action)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (Properties.LauncherSettings.Default.CloseLauncherOnSwitch && LauncherModel.Default.GameManager.GameProcess != null)
                {
                    Task.Run(() => LauncherModel.Default.ShowPrompt_ClosingWithGameStillOpened(action));
                }
                else action.Invoke();
            });

        }
        public async void NavigateToNewProfilePage()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LauncherModel.Default.SetOverlayFrame(new AddProfilePage());
            });

        }

        #endregion
    }
}
