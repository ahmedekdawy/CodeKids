using System.Text;

namespace CodeKids.Infrastructure.WhatsApp;

/// <summary>
/// Replaces <c>{placeholder}</c> tokens inside a message template.
/// </summary>
public static class WhatsAppTemplate
{
    public static string Render(string template, IReadOnlyDictionary<string, string?>? variables)
    {
        if (string.IsNullOrEmpty(template) || variables is null || variables.Count == 0)
        {
            return template;
        }

        var builder = new StringBuilder(template);
        foreach (var (key, value) in variables)
        {
            builder.Replace("{" + key + "}", value ?? string.Empty);
        }

        return builder.ToString();
    }
}
