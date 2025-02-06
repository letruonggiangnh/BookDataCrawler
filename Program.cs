using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Collections.Generic;
using BookDataCrawler;
using System.Xml.Linq;
using System.Linq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using dotenv.net;

namespace WebCrawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var url = @"https://www.fahasa.com/sach-trong-nuoc.html?order=num_orders&limit=24&p=1";

            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                //GetCategoryData();

                GetProduct();

                var htmlContent = FetchHtmlContent(url);

                //ParseHtml(htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void GetProduct()
        {




            string sqlconnectStr = "Data Source=192.168.59.204,1433;Initial Catalog=BookStore;User ID=sa;Password=Qaz@1234;TrustServerCertificate=True";


            string categoryLevel1Id = "4ADCEE60-92E2-4705-AF33-D9257A1786EC";
            string categoryLevel2Id = "DC75FB20-6EE6-4569-9C7D-C1D140C8A515";



            var url = "https://www.fahasa.com/sach-trong-nuoc/thieu-nhi/truyen-thieu-nhi.html?order=num_orders&limit=24&p=1";
            HtmlWeb htmlWeb = new HtmlWeb();
            var htmlDoc = htmlWeb.Load(url);

            var pageNumbers = htmlDoc.DocumentNode
                                    .SelectNodes("//div[@id='pagination']//ol/li/a")
                                    .Select(a => a.InnerText.Trim())
                                    .Where(text => int.TryParse(text, out _))  // Lọc ra những số hợp lệ
                                    .Select(int.Parse)
                                    .ToList();

            int lastPage = pageNumbers.Max();


            for (int i = 1; i <= lastPage; i++)
            {
                var urlProdduct = "https://www.fahasa.com/sach-trong-nuoc/thieu-nhi/truyen-thieu-nhi.html?order=num_orders&limit=24&p=" + i;

                HtmlWeb htmlWebProduct = new HtmlWeb();
                var htmlDocProduct = htmlWeb.Load(urlProdduct);
                HtmlNodeCollection nodes = htmlDocProduct.DocumentNode.SelectNodes("//ul[@id='products_grid']/li");

                foreach (HtmlNode node in nodes)
                {
                    string productLink = node.SelectSingleNode(".//a").GetAttributeValue("href", String.Empty);

                    var productHtml = htmlWeb.Load(productLink);
                    string productName;
                    if (productHtml.DocumentNode.SelectSingleNode(".//h1[@class='fhs_name_product_desktop']") != null)
                    {
                        var h1ProductNameElement = productHtml.DocumentNode.SelectSingleNode(".//h1[@class='fhs_name_product_desktop']");

                        // Lấy toàn bộ văn bản trong h1, bỏ qua phần tử con (div/a)
                        string fullText = h1ProductNameElement.InnerText.Trim();

                        // Loại bỏ từ "Bộ" nếu có
                        productName = fullText.Replace("Bộ", "").Trim();

                        string supplier = "";
                        if (productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa-supplier']/a") != null)
                        {
                            supplier = productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa-supplier']/a").InnerText.Trim();
                        }
                        else
                        {
                            supplier = productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa-supplier']/span[2]").InnerText.Trim();
                        }
                        string author;

                        if (productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa_one']/div[@class='product-view-sa-author']/span[2]") != null)
                        {
                            author = productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa_one']/div[@class='product-view-sa-author']/span[2]").InnerText.Trim();
                        }
                        else
                        {
                            author = "";
                        }

                        string coverType;
                        if (productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa_two']/div[@class='product-view-sa-author']/span[2]") != null)
                        {
                            coverType = productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa_two']/div[@class='product-view-sa-author']/span[2]").InnerText.Trim();
                        }
                        else if (productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa_two']/div[@class='product-view-sa-supplier']/span[2]") != null)
                        {
                            coverType = productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa_two']/div[@class='product-view-sa-supplier']/span[2]").InnerText.Trim();
                        }
                        else
                        {
                            coverType = "";
                        }

                        int price = 0;
                        if (productHtml.DocumentNode.SelectSingleNode(".//div[@class='price-box']/p[@class='old-price']/span[@class='price']") != null)
                        {
                            string priceString = productHtml.DocumentNode.SelectSingleNode(".//div[@class='price-box']/p[@class='old-price']/span[@class='price']").InnerText.Trim();
                            string cleanedPrice = priceString.Replace(".", "").Replace("&nbsp;", "").Replace("đ", "").Trim();
                            price = Int32.Parse(cleanedPrice);
                        }
                        else
                        {
                            string priceString = productHtml.DocumentNode.SelectSingleNode(".//div[@class='price-box']/p[@class='special-price']/span[@class='price']").InnerText.Trim();

                            string cleanedPrice = priceString.Replace(".", "").Replace("&nbsp;", "").Replace("đ", "").Trim();

                            price = Int32.Parse(cleanedPrice);
                        }

                        Console.WriteLine(price + " " + productName + " " + author + " " + supplier + " " + coverType);

                        HtmlNodeCollection productImgNodes = productHtml.DocumentNode.SelectNodes(".//div[@class='product-view-thumbnail']/div[@id='lightgallery-product-media']/a/img");
                        List<string> imgUrl = new List<string>();
                        foreach (var imgNode in productImgNodes)
                        {
                            string urlImage = imgNode.GetAttributeValue("src", "");
                            imgUrl.Add(urlImage);
                        }



                        using (SqlConnection con = new SqlConnection(sqlconnectStr))
                        {
                            con.Open();

                            string checkExistCmd = $"Select count(*) from Book where BookName ={productName}";
                            using (SqlCommand cmd = new SqlCommand(checkExistCmd, con))
                            {

                                int count = (int)cmd.ExecuteScalar();
                                if (count > 0)
                                {
                                    Console.WriteLine("Sản phẩm đã tồn tại.");
                                    continue;
                                }
                                else
                                {
                                    Guid productId = Guid.NewGuid();
                                    string addBookCmd = $"Insert into Book ([Id] ,[BookName] ,[BookPrice],[BookWeight],[BookSupplier] ,[BookRating]           ,[BookInventoryQuantity]        ,[BookSold]          ,[BookDescription]          ,[CategoryLevelOneId]           ,[CategoryLevelTwoId]    ,[Author]           ,[Size]          ,[Language]         ,[NumberOfPages]           ,[CoverType]           ,[Age]           ,[PublicationYear]          ,[Translator]          ,[Description]) values ({productId}, {productName}, {price}, {} )";

                                    using (SqlCommand cmd = new SqlCommand())
                                }
                            }
                        }

                    }
                }
            }

        }

        private static void GetCategoryData()
        {
            var url = @"https://www.fahasa.com/sach-trong-nuoc.html?order=num_orders&limit=24&p=1";
            string sqlconnectStr = "Data Source=192.168.59.198,1433;Initial Catalog=BookStore;User ID=sa;Password=Qaz@1234;TrustServerCertificate=True";
            //string parentId = "1E3642C2-071F-4D0A-809B-F1293C25A12D";
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(url);

            HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//ol[@id='children-categories']/li");

            if (nodes != null && nodes.Count > 0)
            {
                List<string> listCategoryLevel2Name = new List<string>();
                foreach (HtmlNode node in nodes)
                {

                    string categoryLevel2 = node.SelectSingleNode(".//a").GetAttributeValue("title", String.Empty);
                    string categoryLevel3Link = node.SelectSingleNode(".//a").GetAttributeValue("href", String.Empty);
                    Guid categoryLevel2Guid = Guid.NewGuid();
                    string categoryLevel2uidString = categoryLevel2Guid.ToString();

                    if (listCategoryLevel2Name.Contains(categoryLevel2))
                    {
                        break;
                    }

                    using (SqlConnection connection = new SqlConnection(sqlconnectStr))
                    {
                        connection.Open();

                        string query = "INSERT INTO [dbo].[BookCategory] ([Id], [CategoryName]) VALUES (@Id, @CategoryName)";


                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", categoryLevel2uidString);

                            command.Parameters.AddWithValue("@CategoryName", categoryLevel2);



                            command.ExecuteNonQuery();
                        }

                        listCategoryLevel2Name.Add(categoryLevel2);

                        connection.Close();
                    }

                    var htmlCategoryLevel3Doc = web.Load(categoryLevel3Link);

                    HtmlNodeCollection CategoryLevel3nodes = htmlCategoryLevel3Doc.DocumentNode.SelectNodes(".//ol[@id='children-categories']/li");

                    if (CategoryLevel3nodes.Count > 0 && CategoryLevel3nodes != null)
                    {
                        foreach (var categoryLevel3Node in CategoryLevel3nodes)
                        {
                            string categoryLevel3;
                            string categoryLevel4Link;
                            List<string> listCategoryLevel3Name = new List<string>();
                            if (categoryLevel3Node.SelectSingleNode(".//span[@class='m-selected-filter-item']") != null)
                            {
                                categoryLevel3 = categoryLevel3Node.SelectSingleNode(".//span[@class='m-selected-filter-item']").InnerText;
                                if (listCategoryLevel2Name.Contains(categoryLevel3))
                                {
                                    break;
                                }
                                Guid categoryLevel3Guid = Guid.NewGuid();
                                string categoryLevel3uidString = categoryLevel3Guid.ToString();

                                //insert category level 3 
                                using (SqlConnection connection = new SqlConnection(sqlconnectStr))
                                {
                                    connection.Open();
                                    string queryLevel3 = "INSERT INTO [dbo].[BookCategory] ([Id], [CategoryName], [ParentId]) VALUES (@Id, @CategoryName, @ParentId)";

                                    using (SqlCommand command = new SqlCommand(queryLevel3, connection))
                                    {
                                        command.Parameters.AddWithValue("@Id", categoryLevel3uidString);

                                        command.Parameters.AddWithValue("@CategoryName", categoryLevel3);

                                        command.Parameters.AddWithValue("@ParentId", categoryLevel2uidString);


                                        command.ExecuteNonQuery();
                                    }
                                    listCategoryLevel3Name.Add(categoryLevel3);
                                    connection.Close();
                                }
                            }
                            else
                            {
                                categoryLevel3 = categoryLevel3Node.SelectSingleNode(".//a").GetAttributeValue("title", String.Empty);
                                categoryLevel4Link = node.SelectSingleNode(".//a").GetAttributeValue("href", String.Empty);

                                if (listCategoryLevel2Name.Contains(categoryLevel3))
                                {
                                    break;
                                }
                                Guid categoryLevel3Guid = Guid.NewGuid();
                                string categoryLevel3uidString = categoryLevel3Guid.ToString();

                                using (SqlConnection connection = new SqlConnection(sqlconnectStr))
                                {
                                    connection.Open();
                                    string queryLevel3 = "INSERT INTO [dbo].[BookCategory] ([Id], [CategoryName], [ParentId]) VALUES (@Id, @CategoryName, @ParentId)";

                                    using (SqlCommand command = new SqlCommand(queryLevel3, connection))
                                    {
                                        command.Parameters.AddWithValue("@Id", categoryLevel3uidString);

                                        command.Parameters.AddWithValue("@CategoryName", categoryLevel3);

                                        command.Parameters.AddWithValue("@ParentId", categoryLevel2uidString);


                                        command.ExecuteNonQuery();
                                    }
                                    listCategoryLevel3Name.Add(categoryLevel3);
                                    connection.Close();
                                }

                                var htmlCategoryLevel4Doc = web.Load(categoryLevel4Link);

                                HtmlNodeCollection CategoryLevel4nodes = htmlCategoryLevel3Doc.DocumentNode.SelectNodes(".//ol[@id='children-categories']/li");


                                if (CategoryLevel4nodes.Count > 0 && CategoryLevel4nodes != null)
                                {
                                    foreach (var categoryLevel4Node in CategoryLevel4nodes)
                                    {
                                        string categoryLevel4;

                                        if (categoryLevel4Node.SelectSingleNode(".//span[@class='m-selected-filter-item']") != null)
                                        {
                                            categoryLevel4 = categoryLevel3Node.SelectSingleNode(".//span[@class='m-selected-filter-item']").InnerText;
                                            if (listCategoryLevel3Name.Contains(categoryLevel4))
                                            {
                                                break;
                                            }
                                            Guid categoryLevel4Guid = Guid.NewGuid();
                                            string categoryLevel4uidString = categoryLevel4Guid.ToString();


                                            using (SqlConnection connection = new SqlConnection(sqlconnectStr))
                                            {
                                                connection.Open();
                                                string queryLevel3 = "INSERT INTO [dbo].[BookCategory] ([Id], [CategoryName], [ParentId]) VALUES (@Id, @CategoryName, @ParentId)";

                                                using (SqlCommand command = new SqlCommand(queryLevel3, connection))
                                                {
                                                    command.Parameters.AddWithValue("@Id", categoryLevel4uidString);

                                                    command.Parameters.AddWithValue("@CategoryName", categoryLevel4);

                                                    command.Parameters.AddWithValue("@ParentId", categoryLevel3uidString);


                                                    command.ExecuteNonQuery();
                                                }

                                                connection.Close();
                                            }
                                        }
                                        else
                                        {
                                            categoryLevel4 = categoryLevel3Node.SelectSingleNode(".//a").GetAttributeValue("title", String.Empty);
                                            if (listCategoryLevel3Name.Contains(categoryLevel4))
                                            {
                                                break;
                                            }
                                            Guid categoryLevel4Guid = Guid.NewGuid();
                                            string categoryLevel4uidString = categoryLevel4Guid.ToString();

                                            using (SqlConnection connection = new SqlConnection(sqlconnectStr))
                                            {
                                                connection.Open();
                                                string queryLevel3 = "INSERT INTO [dbo].[BookCategory] ([id], [book_category_name], [book_level_one_id]) VALUES (@Id, @CategoryName,@ParentId)";

                                                using (SqlCommand command = new SqlCommand(queryLevel3, connection))
                                                {
                                                    command.Parameters.AddWithValue("@Id", categoryLevel4uidString);

                                                    command.Parameters.AddWithValue("@CategoryName", categoryLevel4);

                                                    command.Parameters.AddWithValue("@ParentId", categoryLevel3uidString);

                                                    command.ExecuteNonQuery();
                                                }
                                                connection.Close();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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
                string bookDetailUrl = htmlNode.SelectSingleNode(".//h2[@class='product-name-no-ellipsis p-name-list']/a").GetAttributeValue("href", string.Empty);
                HtmlWeb web = new HtmlWeb();
                HtmlDocument bookDetailHtml = web.Load(bookDetailUrl);

                string bookName = bookDetailHtml.DocumentNode.SelectSingleNode(".//h1[@class='fhs_name_product_desktop']").InnerText.Trim();

                string bookPrice = bookDetailHtml.DocumentNode.SelectSingleNode(".//p[@class='old-price']/span[@class='price']").InnerText.Trim();

                int bookPriceFormatted = ConvertToNumberFormat(bookPrice);
            }
        }

        static void InsertBookDataToDb(string connectionStr, List<Book> books)
        {

        }

        static int ConvertToNumberFormat(string stringNumberFormat)
        {
            string cleanInput = stringNumberFormat.Replace("&nbsp;", "").Replace("đ", "").Trim();

            if (int.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }
    }
}
