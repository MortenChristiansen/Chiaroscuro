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
            LaunchUrl = args.FirstOrDefault(arg => Uri.IsWellFormedUriString(arg, UriKind.Absolute))
        };
    }
}
