using RazorEngine;
using RazorEngine.Templating;

namespace PdfHtmlTest;

public static class RazorTemplateLoader
{
    private static readonly HashSet<string> templateKeyCache = [];

    public static string LoadTemplate(string templatePath, object model)
    {
        if (!templateKeyCache.Add(templatePath))
            return Engine.Razor.Run(templatePath, null, model);

        string template = File.ReadAllText(templatePath);
        return Engine.Razor.RunCompile(template, templatePath, null, model);
    }
}