using System;

namespace GitLite.Storage;

public class RepoSettings
{
    public string Name { get; set; }
    public string Location { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not RepoSettings settings) return false;
        return Name.Equals(settings.Name, StringComparison.Ordinal) && Location.Equals(settings.Location, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Name.GetHashCode(StringComparison.Ordinal) + Location.GetHashCode(StringComparison.Ordinal);
        }
    }

    public override string ToString()
        => Name;
}