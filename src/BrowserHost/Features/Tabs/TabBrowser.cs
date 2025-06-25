using BrowserHost.Api;
using BrowserHost.Api.Dtos;
using BrowserHost.Handlers;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : ChromiumWebBrowser
{
    public string Id { get; } = $"{Guid.NewGuid()}";

    private readonly BrowserApi _api;

    public TabBrowser(BrowserApi api, string address)
    {
        _api = api;

        Address = address;

        TitleChanged += OnTitleChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _api.UpdateTab(new TabDto(Id, (string)e.NewValue, null));
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Dispatcher.BeginInvoke(() => _api.UpdateTab(new TabDto(Id, Title, addresses.FirstOrDefault())));
    }
}
