﻿using HoudiniSafe.Models;
using HoudiniSafe.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace HoudiniSafe.View.UserControls
{
    /// <summary>
    /// Interaktionslogik für CloudView.xaml
    /// </summary>
    public partial class CloudView : UserControl
    {
        public CloudView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //var viewModel = (MainViewModel)DataContext;
            //viewModel.SelectedCloudItem = e.NewValue as CloudItem;
        }
    }
}
