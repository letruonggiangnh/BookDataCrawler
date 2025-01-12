using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WebCrawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the URL to crawl:");
            var url = @"https://www.fahasa.com/sach-trong-nuoc/van-hoc-trong-nuoc/tieu-thuyet.html";

            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("Invalid URL. Exiting...");
                return;
            }

            try
            {
                Console.WriteLine("Fetching data...");
                var htmlContent = FetchHtmlContent(url);

                Console.WriteLine("Parsing data...");
                ParseHtml(htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetches HTML content from the specified URL.
        /// </summary>
        static HtmlDocument FetchHtmlContent(string url)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(url);

            
            return htmlDoc;
        }
            
        /// <summary>
        /// Parses the HTML and extracts specific data.
        /// </summary>
        static void ParseHtml(HtmlDocument htmlDoc)
        {
           HtmlNode htmlNodes = htmlDoc.DocumentNode.SelectSingleNode("//ul[@id='products_grid']");
           HtmlNodeCollection nodes = htmlNodes.SelectNodes("li");
            foreach (HtmlNode htmlNode in nodes)
            {
                Console.WriteLine(htmlNode.InnerHtml);
                Console.WriteLine("_____________________________________________________");
                string bookName = htmlNode.SelectSingleNode(".//h2[@class='p-name-list']").InnerHtml;
                Console.WriteLine(bookName);
            }

        }
    }
}
