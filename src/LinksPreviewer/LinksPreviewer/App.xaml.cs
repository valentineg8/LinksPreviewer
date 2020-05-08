using System;
using LinksPreviewer.Views;
using Xamarin.Forms;

namespace LinksPreviewer
{
    public partial class App: Application
    {
        public App()
        {
           MainPage = new NavigationPage(new MainPage());
        }
    }
}
