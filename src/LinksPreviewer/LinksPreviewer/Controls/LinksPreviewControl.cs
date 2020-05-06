﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LinksPreviewer.Models;
using Xamarin.Forms;

namespace LinksPreviewer.Controls
{
    public class LinksPreviewControl : ContentView
    {
        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(LinksPreviewControl), default(DataTemplate));
        public static readonly BindableProperty LimitProperty = BindableProperty.Create(nameof(Limit), typeof(int), typeof(LinksPreviewControl), 0 );
        public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(LinksPreviewControl), StackOrientation.Vertical, propertyChanged: OrientationPropertyChanged);
        public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(LinksPreviewControl), 10d, propertyChanged: SpacingPropertyChanged);

        public StackOrientation Orientation
        {
            get { return (StackOrientation)GetValue(LimitProperty); }
            set { SetValue(LimitProperty, value); }
        }

        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        public int Limit
        {
            get { return (int)GetValue(LimitProperty); }
            set { SetValue(LimitProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }


        [TypeConverter(typeof(ReferenceTypeConverter))]
        public InputView InputView
        {
            set => LinkToLinksPreviewControl(this, value);
        }

        static void LinkToLinksPreviewControl(LinksPreviewControl control, InputView inputView)
        {
            if (inputView == null)
                return;

            if (control.HasElement)
               throw new Exception("cannot set Label and InputView");
            control.HasElement = true;
            inputView.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == "Text")
                {
                    control.CreateLinksPreview(inputView.Text);
                }
            };
        }

        [TypeConverter(typeof(ReferenceTypeConverter))]
        public Label Label
        {
            set => LinkToPreviewControl(this, value);
        }

        static void LinkToPreviewControl(LinksPreviewControl control, Label label)
        {
            
            if (label == null)
                return;
            if (control.HasElement)
                throw new Exception("cannot set Label and InputView");
            control.HasElement = true;
            label.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == "Text")
                {
                    control.CreateLinksPreview(label.Text);
                }
            };
        }


        public List<Link> Links { get; set; }
        bool HasElement { get; set; }
        readonly HttpClient Client;
        StackLayout _mainContentLayout;
        public LinksPreviewControl()
        {
            _mainContentLayout = new StackLayout() { Spacing = Spacing };
            Content = _mainContentLayout;
            Links = new List<Link>();
            Client = new HttpClient();
        }
        static void OrientationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var source = bindable as LinksPreviewControl;
            if (source == null)
                return;
            source._mainContentLayout.Orientation = (StackOrientation)newValue;
        }

        private static void SpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var source = bindable as LinksPreviewControl;
            if (source == null)
                return;
            source._mainContentLayout.Spacing = (double)newValue;
        }
        async void CreateLinksPreview(string text)
        {
            var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
            List<Link> newList = new List<Link>();
            var links = linkParser.Matches(text);
            foreach (var item in links)
            {
                if (Limit != 0 && newList.Count == Limit)
                    break;
                string url = item.ToString();
                var link = Links.FirstOrDefault(element => element.URL == url);
                if (link == null)
                {
                    link = await GetLinkData(url);
                    if (link != null)
                    {
                        newList.Add(link);
                        _mainContentLayout.Children.Add(CreateNewItem(link));
                    }
                }
                else
                    newList.Add(link);
            }
            foreach (var item in _mainContentLayout.Children.ToList())
            {
                var link = newList.FirstOrDefault(el => el == item.BindingContext);
                if (link == null)
                    _mainContentLayout.Children.Remove(item);
            }
            Links = newList;
        }

        protected virtual View CreateNewItem(object item)
        {
            View view = null;
            if (ItemTemplate != null)
            {
                var content = ItemTemplate.CreateContent();
                view = (content is View) ? content as View : ((ViewCell)content).View;

                view.BindingContext = item;
            }
            return view;
        }

        async Task<Link> GetLinkData(string url)
        {
            try
            {
                Client.CancelPendingRequests();
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
                else if (metaInformation.ContainsKey("twitter:description"))
                    newLink.Description = metaInformation["twitter:description"];
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
