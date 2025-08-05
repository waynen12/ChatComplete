using System;
using System.IO;
using System.Threading.Tasks;
using KnowledgeEngine.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing PDF Parser...");
        
        // Initialize logger
        LoggerProvider.Initialize();
        
        var pdfPath = "./Docs/System_Inventory_Specification.pdf";
        
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"PDF file not found: {pdfPath}");
            return;
        }
        
        var parser = new PDFKnowledgeSource();
        
        using var fileStream = File.OpenRead(pdfPath);
        var result = await parser.ParseAsync(fileStream);
        
        if (result.Success && result.Document != null)
        {
            var doc = result.Document;
            var headings = doc.Elements.OfType<IHeadingElement>().ToList();
            var paragraphs = doc.Elements.OfType<ParagraphElement>().ToList();
            
            Console.WriteLine($"✅ PDF parsed successfully!");
            Console.WriteLine($"📋 Title: {doc.Title}");
            Console.WriteLine($"📑 Total elements: {doc.Elements.Count}");
            Console.WriteLine($"📝 Headings found: {headings.Count}");
            Console.WriteLine($"📄 Paragraphs found: {paragraphs.Count}");
            
            Console.WriteLine("\n🎯 First 5 headings:");
            foreach (var heading in headings.Take(5))
            {
                Console.WriteLine($"  H{heading.Level}: {heading.Text.Substring(0, Math.Min(80, heading.Text.Length))}");
            }
            
            Console.WriteLine("\n📖 First 3 paragraphs:");
            foreach (var para in paragraphs.Take(3))
            {
                Console.WriteLine($"  {para.Text.Substring(0, Math.Min(100, para.Text.Length))}...");
            }
        }
        else
        {
            Console.WriteLine($"❌ PDF parsing failed: {result.Error}");
        }
    }
}