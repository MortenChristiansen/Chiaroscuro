using System;
using System.Linq;

namespace BrowserHost;

public class Options
{
    public bool ForceAppRegistration { get; set; }
    public string? LaunchUrl { get; set; }

    public static Options Parse(string[] args)
    {
        return new Options
        {
            ForceAppRegistration = args.Contains("--forceAppRegistration"),
            LaunchUrl = GetLaunchUrl(args)
        };
    }

    public static string? GetLaunchUrl(string[] args)
    {
        return args.FirstOrDefault(arg => Uri.IsWellFormedUriString(arg, UriKind.Absolute));
    }
}
