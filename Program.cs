using Dapper;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Data.SqlClient;

namespace PdfHtmlTest;

public class Program
{
    public const string BASE_URI = "src/";
    public const string TARGET = "target/";

    private const string CONNECTION_STRING_PATTERN =
        "Data Source={0},{1};Initial Catalog={2};User Id={3};Password={4};Encrypt=True;TrustServerCertificate=True";

    private const string SQL_QUERY = """
                                     SELECT p.ProductID, p.ProductName, c.CategoryName, p.QuantityPerUnit, p.UnitPrice
                                     FROM Products p JOIN Categories c ON p.CategoryID = c.CategoryID
                                     ORDER BY p.ProductID
                                     """;

    static void Main()
    {
        GenerateStaticPdf("hello.html", 3);
        GenerateStaticPdf("header.html");
        GeneratePdfReport();
        Console.WriteLine("Done!");
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
        var connectionString = string.Format(CONNECTION_STRING_PATTERN,
                                             Environment.GetEnvironmentVariable("DBHOST"),
                                             Environment.GetEnvironmentVariable("DBPORT"),
                                             Environment.GetEnvironmentVariable("DBNAME"),
                                             Environment.GetEnvironmentVariable("DBUSER"),
                                             Environment.GetEnvironmentVariable("DBSECRET"));

        using var connection = new SqlConnection(connectionString);

        var pdfModel = new PdfModel("List of Products",
                                    "Just to see how the subtitle is rendered when supplied",
                                    connection.Query<Product>(SQL_QUERY).ToList());

        var source = Path.Combine(BASE_URI, "products.razor");
        var dest = Path.Combine(TARGET, "products.pdf");

        var properties = new ConverterProperties();
        properties.SetBaseUri(BASE_URI);

        var html = RazorTemplateLoader.LoadTemplate(source, pdfModel);
        using var output = new FileStream(dest, FileMode.Create, FileAccess.Write);
        using var writer = new PdfWriter(output, new WriterProperties().SetFullCompressionMode(true));
        var pdf = new PdfDocument(writer);
        var handler = new ReportBackground(pdf, Path.Combine(TARGET, "header.pdf"));
        pdf.AddEventHandler(PdfDocumentEvent.START_PAGE, handler);
        HtmlConverter.ConvertToPdf(html, pdf, properties);
    }

    class ReportBackground : AbstractPdfDocumentEventHandler
    {
        private PdfXObject stationery;

        public ReportBackground(PdfDocument pdf, string src)
        {
            PdfDocument template = new PdfDocument(new PdfReader(src));
            PdfPage page = template.GetPage(1);
            stationery = page.CopyAsFormXObject(pdf);
            template.Close();
        }

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
        {
            PdfDocument pdf = @event.GetDocument();
            PdfPage page = ((PdfDocumentEvent)@event).GetPage();
            PdfCanvas pdfCanvas = new(page.NewContentStreamBefore(), page.GetResources(), pdf);
            pdfCanvas.AddXObjectAt(stationery, 0, 0);
        }
    }
}