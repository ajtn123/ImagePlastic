using Octokit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ImagePlastic.Utilities;

public static class UpdateChecker
{
    //https://stackoverflow.com/a/65029587
    public static async Task<Version> GetGitHubVersion()
    {
        var appName = Assembly.GetExecutingAssembly().GetName().Name;

        //Get all releases from GitHub
        //Source: https://octokitnet.readthedocs.io/en/latest/getting-started/
        GitHubClient client = new(new ProductHeaderValue(appName));
        IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("ajtn123", appName);
        return new(releases[0].TagName.TrimStart('v'));
    }
    public static async Task<bool> CheckGitHubUpdate()
        => CheckUpdate(await GetGitHubVersion());
    public static bool CheckUpdate(Version newestVersion)
    {
        var localVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new();

        //Compare the Versions
        //Source: https://stackoverflow.com/questions/7568147/compare-version-numbers-without-using-split-function
        int versionComparison = localVersion.CompareTo(newestVersion);

        if (versionComparison < 0) return true;
        else return false;
    }
}
