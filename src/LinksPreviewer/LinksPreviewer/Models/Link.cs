using System;
using System.ComponentModel;

namespace LinksPreviewer.Models
{
    public class Link : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string URL { get; set; }
        public bool Found {
            get
            {
                return !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Description) || !string.IsNullOrEmpty(Image);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
