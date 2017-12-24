﻿using Automaton.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace Automaton.ViewModel
{
    class InitialSetupViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand LoadPackCommand { get; set; }
        public RelayCommand BrowseSourceLocationCommand { get; set; }
        public RelayCommand BrowseInstallationLocationCommand { get; set; }
        public RelayCommand CheckFormStatusCommand { get; set; }
        public RelayCommand NextCardCommand { get; set; }

        public bool IsPackLoaded { get; set; }
        public bool IsFormCompleted { get; set; }

        public string SourceLocation { get; set; }
        public string InstallationLocation { get; set; }

        public InitialSetupViewModel()
        {
            LoadPackCommand = new RelayCommand(LoadPack);
            BrowseSourceLocationCommand = new RelayCommand(BrowseSourceLocation);
            BrowseInstallationLocationCommand = new RelayCommand(BrowseInstallationLocation);
            CheckFormStatusCommand = new RelayCommand(CheckFormStatus);
            NextCardCommand = new RelayCommand(NextCard);

            IsPackLoaded = false;
            IsFormCompleted = false;
        }

        private void LoadPack()
        {
            // Get the path of the .pack file
            var dialog = new OpenFileDialog()
            {
                Title = "Select a packfile",
                Filter = "PACK FILES (*.7z, *.rar, *.zip)|*.7z; *.rar; *.zip;"
            };

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                PackHandler.PackLocation = dialog.FileName;
                var readPackResponse = PackHandler.ReadPack(PackHandler.PackLocation);

                if (readPackResponse != null)
                {
                    IsPackLoaded = true;
                }

                else
                {
                    IsPackLoaded = false;
                }

                CheckFormStatus();
            }
        }

        private void BrowseSourceLocation()
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Select the mod source location."
            };

            var result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                SourceLocation = dialog.FileName;
            }
        }

        private void BrowseInstallationLocation()
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Select an install location."
            };

            var result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                InstallationLocation = dialog.FileName;
            }
        }

        private void CheckFormStatus()
        {
            if (Directory.Exists(SourceLocation) && Directory.Exists(InstallationLocation) && IsPackLoaded)
            {
                IsFormCompleted = true;
            }

            else
            {
                IsFormCompleted = false;
            }
        }

        private void NextCard()
        {
            PackHandler.SourceLocation = SourceLocation;
            PackHandler.InstallationLocation = InstallationLocation;

            var modPack = PackHandler.ModPack;

            // Check if the optional setup has no null required values. 
            if (PackHandler.DoesOptionalExist(modPack))
            {
                Messenger.Default.Send(CardIndex.OptionalSetup);
                return;
            }

            PackHandler.GenerateFinalModPack();

            if (PackHandler.ValidateSourceLocation(SourceLocation).Count > 0)
            {
                Messenger.Default.Send(CardIndex.ModValidation);
                return;
            }

            Messenger.Default.Send(CardIndex.CompletedSetup);
        }
    }
}
