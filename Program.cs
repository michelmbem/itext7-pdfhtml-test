using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using iText.Html2pdf;
using iText.Kernel.Events;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Configuration;

namespace PdfHtmlTest
{
    public class Program
    {
        public const string BASE_URI = "src/";
        public const string TARGET = "target/";
        private const string SQL_QUERY =
                @"SELECT a.AlbumId, a.Title, b.[Name] AS ArtistName
                FROM Album a JOIN Artist b ON a.ArtistId = b.ArtistId
                ORDER BY a.AlbumId";

        private static readonly IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(GetRootPath("AppSettings.json"))
                .Build();

        static void Main()
        {
            GenerateStaticPdf("hello.html", 3);
            GenerateStaticPdf("header.html");
            GeneratePdfReport();
            Console.WriteLine("Done!");
        }

        static string GetRootPath(string rootFilename)
        {
            string rootPath;
            var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var matchThePath = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = matchThePath.Match(rootDir).Value;
            rootPath= Path.Combine(appRoot, rootFilename);
                    
            return rootPath; 
        }

        static void GenerateStaticPdf(string htmlFile, int option = 0)
        {
            string source = Path.Combine(BASE_URI, htmlFile);
            string dest = Path.Combine(TARGET, Path.ChangeExtension(htmlFile, "pdf"));
            
            ConverterProperties properties = new ConverterProperties();
            properties.SetBaseUri(BASE_URI);

            using FileStream input = File.OpenRead(source);
            using FileStream output = File.OpenWrite(dest);

            if (option == 0)
            {
                HtmlConverter.ConvertToPdf(input, output, properties);
            }
            else
            {
                using var writer = new PdfWriter(output, new WriterProperties().SetFullCompressionMode(true));
                
                if (option == 1)
                {
                    HtmlConverter.ConvertToPdf(input, writer, properties);
                }
                else
                {
                    var pdf = new PdfDocument(writer);
                    pdf.SetTagged();
                    
                    if (option == 2)
                    {
                        HtmlConverter.ConvertToPdf(input, pdf, properties);
                    }
                    else
                    {
                        Document doc = HtmlConverter.ConvertToDocument(input, pdf, properties);
                        doc.Add(new Paragraph("Bonjour mes chers!"));
                        doc.Close();
                    }
                }
            }
        }

        static void GeneratePdfReport()
        {
            using var connection = new SqlConnection(configuration.GetSection("ConnectionString")["Chinook"]);
            var pdfModel = new PdfModel
            {
                Title = "List of Albums",
                SubTitle = "Just to see how the subtitle is rendered when supplied",
                Data = connection.Query<AlbumWithAuthorName>(SQL_QUERY).ToList(),
            };
            
            string source = Path.Combine(BASE_URI, "albums.razor");
            string dest = Path.Combine(TARGET, "albums.pdf");

            ConverterProperties properties = new ConverterProperties();
            properties.SetBaseUri(BASE_URI);

            string html = RazorTemplateLoader.LoadTemplate(source, pdfModel);
            using var output = new FileStream(dest, FileMode.Create, FileAccess.Write);
            using var writer = new PdfWriter(output, new WriterProperties().SetFullCompressionMode(true));
            var pdf = new PdfDocument(writer);
            var handler = new ReportBackground(pdf, Path.Combine(TARGET, "header.pdf"));
            pdf.AddEventHandler(PdfDocumentEvent.START_PAGE, handler);
            HtmlConverter.ConvertToPdf(html, pdf, properties);
        }
        
        class ReportBackground : IEventHandler
        {
            private PdfXObject stationery;
        
            public ReportBackground(PdfDocument pdf, string src)
            {
                PdfDocument template = new PdfDocument(new PdfReader(src));
                PdfPage page = template.GetPage(1);
                stationery = page.CopyAsFormXObject(pdf);
                template.Close();
            }
        
            public void HandleEvent(Event @event)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent) @event;
                PdfDocument pdf = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                PdfCanvas pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdf);
                pdfCanvas.AddXObjectAt(stationery, 0, 0);
            }
        }
    }
}