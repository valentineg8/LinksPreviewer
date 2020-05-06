using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using LinksPreviewer.Models;
using Prism.Mvvm;

namespace LinksPreviewer.ViewModels
{
    public class MainPageViewModel : BindableBase, INotifyPropertyChanged
    {
        public ObservableCollection<Link> Links { get; set; }

        public Link EditorLink { get; set; }
        public MainPageViewModel()
        {
        }
    }
}
