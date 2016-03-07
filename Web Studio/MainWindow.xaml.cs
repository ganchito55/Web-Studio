﻿using System.Windows;
using Web_Studio.Events;

namespace Web_Studio
{
    /// <summary>
    ///     Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            EventSystem.Subscribe<ChangedLanguageEvent>(ManageChangedLanguage);

        }

        /// <summary>
        /// Show explorer control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExplorerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ExplorerLayout.Show();
        }

        /// <summary>
        /// Method to manage the changed language event
        /// </summary>
        /// <param name="obj"></param>
        private void ManageChangedLanguage(ChangedLanguageEvent obj)
        {
            BusyControl.RefreshUI();
            MessageListControl.RefreshUI();
        }
    }
}