﻿using BedrockLauncher.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BedrockLauncher.Pages.Settings
{
    public partial class SettingsTabs : Page
    {
        public GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        public AccountsSettingsPage accountsSettingsPage = new AccountsSettingsPage();
        public VersionsPage versionsSettingsPage = new VersionsPage();
        public AboutPage aboutPage = new AboutPage();
        public SettingsTabs()
        {
            InitializeComponent();
            ButtonManager_Base(GeneralButton.Name);
        }

        private async void Navigate(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;

            if (!animate)
            {
                await SettingsScreenFrame.Dispatcher.InvokeAsync(() => SettingsScreenFrame.Navigate(content));
                return;
            }

            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex_Settings;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex_Settings;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => BedrockLauncher.Core.Components.PageAnimator.FrameSwipe(SettingsScreenFrame, content, direction));
        }

        #region Navigation

        public void ResetButtonManager(string buttonName)
        {
            this.Dispatcher.Invoke(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                GeneralButton,
                VersionsButton,
                AccountsButton,
                AboutButton
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
        public void ButtonManager_Base(string senderName)
        {
            this.Dispatcher.Invoke(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == GeneralButton.Name) NavigateToGeneralPage();
                else if (senderName == AccountsButton.Name) NavigateToAccountsPage();
                else if (senderName == AboutButton.Name) NavigateToAboutPage();
                else if (senderName == VersionsButton.Name) NavigateToVersionsPage();
            });
        }

        public void NavigateToGeneralPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(0);
            Task.Run(() => Navigate(generalSettingsPage));
        }
        public void NavigateToVersionsPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(1);
            Task.Run(() => Navigate(versionsSettingsPage));
        }

        public void NavigateToAccountsPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(2);
            Task.Run(() => Navigate(accountsSettingsPage));
        }

        public void NavigateToAboutPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(3);
            Task.Run(() => Navigate(aboutPage));
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
