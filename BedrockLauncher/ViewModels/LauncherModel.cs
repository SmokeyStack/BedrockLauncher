﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Events;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using BedrockLauncher.Methods;
using System.Windows.Controls;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.Community;
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Play;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.FirstLaunch;
using System.Windows.Media.Animation;
using BedrockLauncher.Core.Components;
using BedrockLauncher.Core.Pages.Common;
using BedrockLauncher.Core.Interfaces;
using BedrockLauncher.Downloaders;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Core.Classes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BedrockLauncher.ViewModels
{
    public class LauncherModel : NotifyPropertyChangedBase, IDialogHander, ILauncherModel
    {
        #region Init

        public static LauncherModel Default { get; set; } = new LauncherModel();
        public static MainWindow MainThread
        {
            get
            {
                return App.Current.Dispatcher.Invoke(() =>
                {
                    return (MainWindow)App.Current.MainWindow;
                });
            }
        }
        public static LauncherUpdater Updater { get; set; } = new LauncherUpdater();

        public LauncherModel()
        {
            ProgressBarStateChanged += ViewModel_ProgressBarStateChanged;
            ErrorScreenShow.SetHandler(this);
            DialogPrompt.SetHandler(this);
        }

        public void InitKeyboardFocus(Grid MainFrame)
        {
            LauncherModel.Default.KeyboardNavigationMode = KeyboardNavigation.GetTabNavigation(MainFrame);
        }

        #endregion

        #region Other Models
        public FilepathManager FilepathManager { get; private set; } = new FilepathManager();
        public GameManager GameManager { get; private set; } = new GameManager();
        public VersionDownloader VersionDownloader { get; private set; } = new VersionDownloader();

        #endregion

        #region Page Indexes

        public int CurrentPageIndex { get; private set; } = -2;
        public int LastPageIndex { get; private set; } = -1;

        public int CurrentPageIndex_News { get; private set; } = -2;
        public int LastPageIndex_News { get; private set; } = -1;

        public int CurrentPageIndex_Play { get; private set; } = -2;
        public int LastPageIndex_Play { get; private set; } = -1;

        public int CurrentPageIndex_Settings { get; private set; } = -2;
        public int LastPageIndex_Settings { get; private set; } = -1;

        public void UpdatePageIndex(int index)
        {
            LastPageIndex = CurrentPageIndex;
            CurrentPageIndex = index;
        }

        public void UpdatePlayPageIndex(int index)
        {
            LastPageIndex_Play = CurrentPageIndex_Play;
            CurrentPageIndex_Play = index;
        }

        public void UpdateNewsPageIndex(int index)
        {
            LastPageIndex_News = CurrentPageIndex_News;
            CurrentPageIndex_News = index;
        }

        public void UpdateSettingsPageIndex(int index)
        {
            LastPageIndex_Settings = CurrentPageIndex_Settings;
            CurrentPageIndex_Settings = index;
        }

        #endregion

        #region UI

        private void ViewModel_ProgressBarStateChanged(object sender, EventArgs e)
        {
            Events.ProgressBarState es = e as Events.ProgressBarState;
            if (es.isVisible) ProgressBarShowAnim();
            else ProgressBarHideAnim();
        }


        private void UpdateProgressBarContent(StateChange value)
        {
            Application.Current.Dispatcher.Invoke(() => {
                switch (value)
                {
                    case StateChange.isInitializing:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isDownloading:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isExtracting:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Extracting");
                        break;
                    case StateChange.isRegisteringPackage:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RegisteringPackage");
                        break;
                    case StateChange.isRemovingPackage:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RemovingPackage");
                        break;
                    case StateChange.isUninstalling:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Uninstalling");
                        break;
                    case StateChange.isLaunching:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Launching");
                        break;
                    case StateChange.isBackingUp:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_BackingUp");
                        break;
                }

            });
        }
        private void ProgressBarUpdate()
        {
            ProgressBarUpdate(_CurrentProgress, _TotalProgress);
        }
        private void ProgressBarUpdate(double downloadedBytes, double totalSize)
        {
            bool isAdvanced = Properties.LauncherSettings.Default.ShowAdvancedInstallDetails;
            bool isIndeterminate = (CurrentState == StateChange.isInitializing || 
                CurrentState == StateChange.isUninstalling || 
                CurrentState == StateChange.isLaunching || 
                (!isAdvanced ? (CurrentState == StateChange.isRemovingPackage || CurrentState == StateChange.isRegisteringPackage) : false));

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MainThread != null)
                {
                    MainThread.progressSizeHack.Value = downloadedBytes;
                    MainThread.progressSizeHack.Maximum = totalSize;

                    MainThread.BedrockEditionButton.progressSizeHack.Value = downloadedBytes;
                    MainThread.BedrockEditionButton.progressSizeHack.Maximum = totalSize;

                    MainThread.ProgressBarText.Text = DisplayStatus;

                    MainThread.progressSizeHack.IsIndeterminate = isIndeterminate;
                    MainThread.BedrockEditionButton.progressSizeHack.IsIndeterminate = isIndeterminate;
                }
            });
        }

        private async void ProgressBarShowAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
                MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
                MainThread.ProgressBarText.Visibility = Visibility.Hidden;
                MainThread.progressbarcontent.Visibility = Visibility.Hidden;

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 0,
                    To = 72,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, MainThread.ProgressBarGrid);
                storyboard.Completed += new EventHandler(ShowProgressBarContent);
                storyboard.Begin();
            });
        }
        private async void ProgressBarHideAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Hidden;
                MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
                MainThread.ProgressBarText.Visibility = Visibility.Hidden;
                MainThread.progressbarcontent.Visibility = Visibility.Hidden;

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 72,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, MainThread.ProgressBarGrid);
                storyboard.Completed += new EventHandler(HideProgressBarContent);
                storyboard.Begin();
            });
        }

        private void HideProgressBarContent(object sender, EventArgs e)
        {
            MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Hidden;
            MainThread.ProgressBarGrid.Visibility = Visibility.Collapsed;
            MainThread.ProgressBarText.Visibility = Visibility.Hidden;
            MainThread.progressbarcontent.Visibility = Visibility.Hidden;
        }
        private void ShowProgressBarContent(object sender, EventArgs e)
        {
            MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
            MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
            MainThread.ProgressBarText.Visibility = Visibility.Visible;
            MainThread.progressbarcontent.Visibility = Visibility.Visible;
        }

        public void UpdateUI()
        {
            OnPropertyChanged(nameof(IsGameRunning));
            OnPropertyChanged(nameof(ShowProgressBar));
            OnPropertyChanged(nameof(PlayButtonString));
            OnPropertyChanged(nameof(AllowPlaying));
            OnPropertyChanged(nameof(AllowEditing));
        }


        #endregion

        #region Prompts



        private bool AllowedToCloseWithGameOpen { get; set; } = false;
        private KeyboardNavigationMode KeyboardNavigationMode { get; set; }

        public async void ShowPrompt_ClosingWithGameStillOpened(Action successAction)
        {
            await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(async () =>
            {
                var title = MainThread.FindResource("Dialog_CloseGame_Title") as string;
                var content = MainThread.FindResource("Dialog_CloseGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNoCancel(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    LauncherModel.Default.GameManager.GameProcess.Kill();
                    AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.No)
                {
                    AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.Cancel)
                {
                    AllowedToCloseWithGameOpen = false;
                }

            }));
        }
        public void AttemptClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Action action = new Action(() =>
            {
                MainThread.Close();
            });

            if (Properties.LauncherSettings.Default.KeepLauncherOpen && LauncherModel.Default.GameManager.GameProcess != null)
            {
                if (!AllowedToCloseWithGameOpen)
                {
                    e.Cancel = true;
                    ShowPrompt_ClosingWithGameStillOpened(action);
                }
            }
            else
            {
                e.Cancel = false;
            }
        }
        public async void SetDialogFrame(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;
            bool isEmpty = content == null;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var focusMode = (isEmpty ? KeyboardNavigationMode : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainThread.MainFrame, focusMode);
                KeyboardNavigation.SetTabNavigation(MainThread.OverlayFrame, focusMode);
                Keyboard.ClearFocus();
            });

            if (animate)
            {
                if (isEmpty) await PageAnimator.FrameFadeOut(MainThread.ErrorFrame, content);
                else await PageAnimator.FrameFadeIn(MainThread.ErrorFrame, content);
            }
            else await PageAnimator.Navigate(MainThread.ErrorFrame, content);
        }

        public void SetOverlayFrame_Strict(object content)
        {
            SetOverlayFrame_Base(content, false);
        }

        public void SetOverlayFrame(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;
            SetOverlayFrame_Base(content, animate);
        }


        private async void SetOverlayFrame_Base(object content, bool animate)
        {
            bool isEmpty = content == null;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var focusMode = (isEmpty ? KeyboardNavigationMode : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainThread.MainFrame, focusMode);
                Keyboard.ClearFocus();
            });

            if (animate)
            {
                if (isEmpty) await PageAnimator.FrameSwipe_OverlayOut(MainThread.OverlayFrame, content);
                else await PageAnimator.FrameSwipe_OverlayIn(MainThread.OverlayFrame, content);
            }
            else await PageAnimator.Navigate(MainThread.OverlayFrame, content);
        }



        #endregion

        #region Enums
        public enum StateChange
        {
            None,
            isInitializing,
            isExtracting,
            isUninstalling,
            isLaunching,
            isDownloading,
            isBackingUp,
            isRemovingPackage,
            isRegisteringPackage
        }

        #endregion

        #region Events

        public event EventHandler ProgressBarStateChanged;
        protected virtual void OnProgressBarStateChanged(ProgressBarState e)
        {
            EventHandler handler = ProgressBarStateChanged;
            if (handler != null) handler(this, e);
        }

        public event EventHandler ConfigUpdated;
        protected virtual void OnConfigUpdated(PropertyChangedEventArgs e)
        {
            EventHandler handler = ConfigUpdated;
            if (e.PropertyName == nameof(Config.CurrentInstallations))
                if (handler != null) handler(this, e);
        }

        #endregion

        #region One Way Bindings
        public string PlayButtonString
        {
            get
            {         
                if (IsGameRunning) return App.Current.FindResource("GameTab_PlayButton_Kill_Text").ToString();
                else return App.Current.FindResource("GameTab_PlayButton_Text").ToString();
            }
        }
        public bool AllowEditing
        {
            get
            {
                return AllowPlaying && !_IsGameRunning && !ShowProgressBar;
            }
        }
        public bool AllowPlaying
        {
            get
            {
                return CurrentState == StateChange.None && !ShowProgressBar;
            }
        }
        public string DisplayStatus
        {
            get
            {
                switch (CurrentState)
                {
                    case StateChange.isInitializing:
                        return "";
                    case StateChange.isDownloading:
                        return DownloadStatus;
                    case StateChange.isExtracting:
                        return ExtractingStatus;
                    case StateChange.isRegisteringPackage:
                        return DeploymentStatus;
                    case StateChange.isRemovingPackage:
                        return DeploymentStatus;
                    case StateChange.isUninstalling:
                        return "";
                    case StateChange.isLaunching:
                        return "";
                    case StateChange.isBackingUp:
                        return BackupStatus;
                    case StateChange.None:
                        return "";
                    default:
                        return "";
                }
            }
        }
        private string BackupStatus
        {
            get
            {
                return string.Format("{0} / {1}", CurrentProgress, TotalProgress);
            }
        }
        private string DeploymentStatus
        {
            get
            {
                return CurrentProgress + "%" + string.Format(" [ {0} ]", DeploymentPackageName);
            }
        }
        private string ExtractingStatus
        {
            get
            {
                long percent = 0;
                if (TotalProgress != 0) percent = (100 * CurrentProgress) / TotalProgress;
                return percent + "%";
            }
        }
        private string DownloadStatus
        {
            get
            {
                return (Math.Round((double)CurrentProgress / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round((double)TotalProgress / 1024 / 1024, 2)).ToString() + " MB";
            }
        }

        #endregion

        #region Bindings

        private bool _IsGameRunning = false;
        private bool _ShowProgressBar = false;
        private bool _AllowCancel = false;
        private long _CurrentProgress;
        private long _TotalProgress;
        private StateChange _currentState = StateChange.None;

        public StateChange CurrentState
        {
            get { return _currentState; }
            set
            {
                bool IsAdvancedDetail = value == StateChange.isRegisteringPackage || value == StateChange.isRemovingPackage;
                if (!Properties.LauncherSettings.Default.ShowAdvancedInstallDetails && IsAdvancedDetail) return;
                else
                {
                    _currentState = value;
                    OnPropertyChanged(nameof(DisplayStatus));
                    UpdateProgressBarContent(value);
                    ProgressBarUpdate();
                }
            }
        }
        public bool AllowCancel
        {
            get
            {
                return _AllowCancel;
            }
            set
            {
                _AllowCancel = value;
                OnPropertyChanged(nameof(AllowCancel));
                UpdateUI();
            }
        }
        public bool IsGameRunning
        {
            get
            {
                return _IsGameRunning;
            }
            set
            {
                _IsGameRunning = value;
                OnPropertyChanged(nameof(IsGameRunning));
                UpdateUI();
            }
        }
        public bool ShowProgressBar
        {
            get
            {
                return _ShowProgressBar;
            }
            set
            {
                _ShowProgressBar = value;
                OnProgressBarStateChanged(new ProgressBarState(value));
                OnPropertyChanged(nameof(ShowProgressBar));
                UpdateUI();
            }
        }
        public long CurrentProgress
        {
            get
            {
                return _CurrentProgress;
            }
            set
            {
                _CurrentProgress = value;
                ProgressBarUpdate(_CurrentProgress, _TotalProgress);
                OnPropertyChanged(nameof(CurrentProgress));
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateUI();
            }
        }
        public long TotalProgress
        {
            get
            {
                return _TotalProgress;
            }
            set
            {
                _TotalProgress = value;
                OnPropertyChanged(nameof(TotalProgress));
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateUI();
            }
        }

        #endregion

        #region Definitions

        public const long DeploymentMaximum = 100;
        public ICommand CancelCommand { get; set; }
        public string DeploymentPackageName { get; set; }

        #endregion

        #region ConfigManager Porting

        #region Enums

        public enum SortBy_Installation : int
        {
            LatestPlayed,
            Name
        }

        #endregion

        #region Variables


        public string CurrentInstallationUUID
        {
            get
            {
                return Config.CurrentInstallationUUID;
            }
            set
            {
                Config.CurrentInstallationUUID = value;
                OnPropertyChanged(nameof(CurrentInstallationUUID));
            }
        }
        public string CurrentProfileUUID
        {
            get
            {
                return Config.CurrentProfileUUID;
            }
            set
            {
                Config.CurrentProfileUUID = value;
                OnPropertyChanged(nameof(CurrentProfileUUID));
                OnPropertyChanged(nameof(CurrentInstallationUUID));
            }
        }

        public ObservableCollection<BLVersion> Versions { get; private set; } = new ObservableCollection<BLVersion>();
        public MCProfilesList Config { get; private set; } = new MCProfilesList();
        public SortBy_Installation Installations_SortFilter { get; set; } = SortBy_Installation.LatestPlayed;
        public string Installations_SearchFilter { get; set; } = string.Empty;

        #region public bool IsVersionsUpdating

        private bool _IsVersionsUpdating = false;
        public bool IsVersionsUpdating
        {
            get { return _IsVersionsUpdating; }
            set { _IsVersionsUpdating = value; OnPropertyChanged(nameof(IsVersionsUpdating)); }
        }

        #endregion

        #endregion

        #region Methods


        public void LoadConfig()
        {
            Config.PropertyChanged -= (sender, e) => OnConfigUpdated(e);
            Config = MCProfilesList.Load(LauncherModel.Default.FilepathManager.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfile, Properties.LauncherSettings.Default.CurrentProfile);
            Config.PropertyChanged += (sender, e) => OnConfigUpdated(e);
        }

        public async Task LoadVersions()
        {
            if (IsVersionsUpdating) return;
            await Application.Current.Dispatcher.Invoke((Func<Task>)(async () =>
            {
                IsVersionsUpdating = true;
                await LauncherModel.Default.VersionDownloader.UpdateVersions(Versions);
                IsVersionsUpdating = false;
            }));
        }

        #endregion

        #region Filters/Sorting

        public bool Filter_InstallationList(object obj)
        {
            BLInstallation v = obj as BLInstallation;
            if (v == null) return false;
            else if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else if (!v.DisplayName.Contains(Installations_SearchFilter)) return false;
            else return true;
        }
        public void Sort_InstallationList(ref CollectionView view)
        {
            view.SortDescriptions.Clear();
            if (Installations_SortFilter == SortBy_Installation.LatestPlayed) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("LastPlayedT", System.ComponentModel.ListSortDirection.Descending));
            if (Installations_SortFilter == SortBy_Installation.Name) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("DisplayName", System.ComponentModel.ListSortDirection.Ascending));
        }
        public bool Filter_VersionList(object obj)
        {
            BLVersion v = BLVersion.Convert(obj as MCVersion);

            if (v != null && v.IsInstalled)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }

        #endregion

        #endregion
    }
}
