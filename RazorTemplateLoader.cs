using RazorEngine;
using RazorEngine.Templating;

namespace PdfHtmlTest
{
    public static class RazorTemplateLoader
    {
        private static readonly HashSet<string> templateKeyCache = new HashSet<string>();
        
        public static string LoadTemplate(string templatePath, object model)
        {
            if (templateKeyCache.Contains(templatePath))
                return Engine.Razor.Run(templatePath, null, model);

            templateKeyCache.Add(templatePath);
            string template = File.ReadAllText(templatePath);
            return Engine.Razor.RunCompile(template, templatePath, null, model);
        }
    }
}