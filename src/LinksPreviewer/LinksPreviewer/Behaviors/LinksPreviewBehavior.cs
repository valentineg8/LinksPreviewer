using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LinksPreviewer.Models;
using Xamarin.Forms;

namespace LinksPreviewer.Behaviors
{
    public class LinksPreviewBehavior : Behavior<View>
    {
        public static readonly BindableProperty LinksProperty =
        BindableProperty.Create(nameof(Links), typeof(ObservableCollection<Link>), typeof(LinksPreviewBehavior), new ObservableCollection<Link>(), BindingMode.OneWayToSource);
        public ObservableCollection<Link> Links
        {
            get { return (ObservableCollection<Link>)GetValue(LinksProperty); }
            set { SetValue(LinksProperty, value); }
        }

        public static readonly BindableProperty LinkProperty =
        BindableProperty.Create(nameof(Link), typeof(Link), typeof(LinksPreviewBehavior), new Link(), BindingMode.OneWayToSource);
        public Link Link
        {
            get { return (Link)GetValue(LinkProperty); }
            set { SetValue(LinkProperty, value); }
        }

        public static readonly BindableProperty HasMultipleProperty =
        BindableProperty.Create(nameof(HasMultiple), typeof(bool), typeof(LinksPreviewBehavior), false, BindingMode.OneWayToSource);
        public bool HasMultiple
        {
            get { return (bool)GetValue(HasMultipleProperty); }
            set { SetValue(HasMultipleProperty, value); }
        }

        public View AssociatedObject { get; private set; }

        HttpClient Client;
        protected override void OnAttachedTo(View bindable)
        {
            Client = new HttpClient();
            AssociatedObject = bindable;
            if (bindable is Entry entry)
            {
                entry.TextChanged += AssociatedLabel_TextChanged;
            }
            else if (bindable is Editor editor)
            {
                editor.TextChanged += AssociatedLabel_TextChanged;
            }

            //Pending to Suport Labels

            bindable.BindingContextChanged += OnBindingContextChanged;
            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            Client = null;
            AssociatedObject = null;
            if (bindable is Entry entry)
            {
                entry.TextChanged -= AssociatedLabel_TextChanged;
            }
            if (bindable is Editor editor)
            {
                editor.TextChanged -= AssociatedLabel_TextChanged;
            }
            bindable.BindingContextChanged -= OnBindingContextChanged;
            base.OnDetachingFrom(bindable);
        }

        void OnBindingContextChanged(object sender, EventArgs e)
        {
            OnBindingContextChanged();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            BindingContext = AssociatedObject.BindingContext;
        }

        private async void AssociatedLabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
            var links = linkParser.Matches(e.NewTextValue);
            foreach (var item in links)
            {
                string url = item.ToString();
                if (HasMultiple)
                {
                    if (Links.FirstOrDefault(element => element.URL == url) == null)
                    {
                        var newlink = await GetLinkData(url);
                        if (newlink != null)
                            Links.Add(newlink);
                    }
                }
                else
                {
                    if(Link.URL != url)
                    {
                        var response = await GetLinkData(url);
                        Link = response ?? new Link { URL = url };
                    }
                        
                    break;
                }
                
            }
        }

        async Task<Link> GetLinkData(string url)
        {
            try
            {
                Client.CancelPendingRequests();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                var html = await Client.GetStringAsync(url);
                html = Regex.Replace(html, @"\t|\n|\r", "");
                Regex metaTag = new Regex("<meta[\\s]+[^>]*?[property|name]?=[\\s\"\']+(.*?)[\"\']+.*?content[\\s]?=[\\s\"\']+(.*?)[\"\']+.*?>");
                Dictionary<string, string> metaInformation = new Dictionary<string, string>();
                var iyte = metaTag.Matches(html);
                foreach (Match m in iyte)
                {
                    if (!metaInformation.ContainsKey(m.Groups[1].Value))
                        metaInformation.Add(m.Groups[1].Value, m.Groups[2].Value);
                }

                Link newLink = new Link { URL = url };

                if (metaInformation.ContainsKey("og:title"))
                    newLink.Title = metaInformation["og:title"];
                else
                    newLink.Title = Regex.Match(html, "(?<=<title>)(.*?)(?=</title>)").ToString();

                if (metaInformation.ContainsKey("og:description"))
                    newLink.Description = metaInformation["og:description"];
                else if (metaInformation.ContainsKey("description"))
                    newLink.Description = metaInformation["description"];

                if (metaInformation.ContainsKey("og:image"))
                    newLink.Image = metaInformation["og:image"];


                return newLink;
                

            }
            catch
            {
                return null;
            }
            
        }
    }
}
