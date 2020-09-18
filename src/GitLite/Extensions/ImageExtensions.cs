using System;

namespace GitLite.Extensions;

public static class ImageExtensions
{
    public static Uri GetGravatar(string email) =>
        new($"https://www.gravatar.com/avatar/{CreateMd5(email.ToLower())}?d=identicon");

    private static string CreateMd5(string input)
    {
        // Use input string to calculate MD5 hash
        using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }
}