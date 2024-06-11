#nullable enable
using System.Runtime.InteropServices;
using System.Text;

internal sealed class ConfigurationFileReader
{
    private readonly string _path;

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder result, int size, string filePath);

    public ConfigurationFileReader(string path)
    {
        _path = path;
    }

    public string Read(string section, string key)
    {
        var result = new StringBuilder(255);
        GetPrivateProfileString(section, key, "", result, 255, _path);
        return result.ToString();
    }
}
