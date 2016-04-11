﻿using System.Windows.Controls;

namespace ImageScalingPlugin
{
    /// <summary>
    ///     Lógica de interacción para View.xaml
    /// </summary>
    public partial class View : UserControl
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="vm"></param>
        public View(ImageScaling vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}