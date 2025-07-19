using System;

namespace BrowserHost.Features.Tabs;

public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);
