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

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void GetProduct()
        {
            string sqlconnectStr = "Data Source=192.168.59.146,1433;Initial Catalog=BookStore;User ID=sa;Password=Qaz@1234;TrustServerCertificate=True";

            string categoryLevel1Id = "950B36EE-1352-477A-BA58-8C2539D9A933";
            string categoryLevel2Id = "4FD62C9D-0E18-423E-8BAC-78767900D25E";

            var url = "https://www.fahasa.com/sach-trong-nuoc/nuoi-day-con/cam-nang-lam-cha-me.html?order=num_orders&limit=24&p=1";
            HtmlWeb htmlWeb = new HtmlWeb();
            var htmlDoc = htmlWeb.Load(url);

            List<int> pageNumbers = new List<int>();
            
            if(htmlDoc.DocumentNode.SelectNodes("//div[@id='pagination']//ol/li/a") != null)
            {
                pageNumbers = htmlDoc.DocumentNode
                                        .SelectNodes("//div[@id='pagination']//ol/li/a")
                                        .Select(a => a.InnerText.Trim())
                                        .Where(text => int.TryParse(text, out _))
                                        .Select(int.Parse)
                                        .ToList();
            }
            else
            {
                pageNumbers.Add(1);
            }

            int lastPage = pageNumbers.Max();

            for (int i = 1; i <= lastPage; i++)
            {
                StringBuilder sb = new StringBuilder("https://www.fahasa.com/sach-trong-nuoc/nuoi-day-con/cam-nang-lam-cha-me.html?order=num_orders&limit=24&p=");
                var urlProdduct = sb.Append(i);

                HtmlWeb htmlWebProduct = new HtmlWeb();
                var htmlDocProduct = htmlWeb.Load(urlProdduct.ToString());
                HtmlNodeCollection nodes = htmlDocProduct.DocumentNode.SelectNodes("//ul[@id='products_grid']/li");

                foreach (HtmlNode node in nodes)
                {
                    string productLink = node.SelectSingleNode(".//a").GetAttributeValue("href", String.Empty);

                    var productHtml = htmlWeb.Load(productLink);
                    string productName;
                    if (productHtml.DocumentNode.SelectSingleNode(".//h1[@class='fhs_name_product_desktop']") != null)
                    {
                        Guid productId = Guid.NewGuid();
                        var h1ProductNameElement = productHtml.DocumentNode.SelectSingleNode(".//h1[@class='fhs_name_product_desktop']");

                        // Lấy toàn bộ văn bản trong h1, bỏ qua phần tử con (div/a)
                        string fullText = h1ProductNameElement.InnerText.Trim();

                        // Loại bỏ từ "Bộ" nếu có
                        productName = fullText.Replace("Bộ", "").Trim();
                        Console.WriteLine(productName + " " + i);

                        string supplier = "";
                        if (productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa-supplier']/a") != null)
                        {
                            supplier = productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa-supplier']/a").InnerText.Trim();
                        }
                        else if(productHtml.DocumentNode.SelectSingleNode(".//div[@class='product-view-sa-supplier']/span[2]") != null)
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


                        HtmlNodeCollection productImgNodes = productHtml.DocumentNode.SelectNodes(".//div[@class='product-view-thumbnail']/div[@id='lightgallery-product-media']/a/img");
                        List<string> imgUrls = new List<string>();
                        foreach (var imgNode in productImgNodes)
                        {
                            string urlImage = imgNode.GetAttributeValue("src", "");
                            imgUrls.Add(urlImage);
                        }

                        HtmlNodeCollection productInforList = productHtml.DocumentNode.SelectNodes(".//div[@id='product_view_info']/div[@class='product_view_tab_content_ad']/div[@class='product_view_tab_content_additional']/table[@class='data-table table-additional']/tbody/tr");



                        Int64 productWeight = 0, numberOfPages = 0;
                        string size = "", age = "", publicationYear = "", language = "", translator = "";
                        if (productInforList != null)
                        {

                            foreach (HtmlNode infor in productInforList)
                            {
                                HtmlNode thNode = infor.SelectSingleNode("./th");
                                if (thNode.InnerText.Trim() == "Trọng lượng (gr)")
                                {
                                    HtmlNode tdNode = infor.SelectSingleNode("./td");
                                    if (!string.IsNullOrWhiteSpace(tdNode.InnerText))
                                    {
                                        productWeight = Convert.ToInt64(tdNode.InnerText.Trim());
                                    }
                                }
                                else if (thNode.InnerText.Trim() == "Kích Thước Bao Bì")
                                {
                                    size = infor.SelectSingleNode("./td").InnerText.Trim();
                                }
                                else if (thNode.InnerText.Trim() == "Độ Tuổi")
                                {
                                    age = infor.SelectSingleNode("./td").InnerText.Trim();
                                }
                                else if (thNode.InnerText.Trim() == "Năm XB")
                                {
                                    publicationYear = infor.SelectSingleNode("./td").InnerText.Trim();
                                }
                                else if (thNode.InnerText.Trim() == "Ngôn Ngữ")
                                {
                                    language = infor.SelectSingleNode("./td").InnerText.Trim();
                                }
                                else if (thNode.InnerText.Trim() == "Số trang")
                                {

                                    if (!string.IsNullOrEmpty(infor.SelectSingleNode("./td").InnerText))
                                    {
                                        if (!string.IsNullOrEmpty(infor.SelectSingleNode("./td").InnerText.Trim()))
                                        {
                                            numberOfPages = Convert.ToInt32(infor.SelectSingleNode("./td").InnerText.Trim());
                                        }
                                    }
                                }
                                else if (thNode.InnerText.Trim() == "Người Dịch")
                                {
                                    translator = infor.SelectSingleNode("./td").InnerText.Trim();
                                }

                            }
                        }


                        string productDesc = "";
                        if (productHtml.DocumentNode.SelectSingleNode(".//div[@id='desc_content']") != null)
                        {
                            productDesc = HtmlEntity.DeEntitize(productHtml.DocumentNode.SelectSingleNode(".//div[@id='desc_content']").InnerText.Trim());
                        }
                        using (SqlConnection con = new SqlConnection(sqlconnectStr))
                        {
                            con.Open();

                            string checkExistCmd = "Select count(*) from Book where BookName = @BookName";

                            using (SqlCommand cmd = new SqlCommand(checkExistCmd, con))
                            {
                                cmd.Parameters.AddWithValue("@BookName", productName);
                                int count = (int)cmd.ExecuteScalar();
                                if (count > 0)
                                {
                                    continue;
                                }
                                else
                                {

                                    string addBookCmd = "Insert into Book ([Id] ,[BookName] ,[BookPrice],[BookWeight],[BookSupplier],[BookDescription], [CategoryLevelOneId], [CategoryLevelTwoId], [Author],[Size]   ,[Language]         ,[NumberOfPages]           ,[CoverType]           ,[Age]           ,[PublicationYear], [Translator]) values (@Id, @BookName, @BookPrice , @BookWeight, @BookSupplier, @BookDescription, @CategoryLevelOneId, @CategoryLevelTwoId, @Author, @Size, @Language, @NumberOfPages, @CoverType, @Age, @PublicationYear, @Translator)";

                                    string addImageCmd = "Insert into BookImage([product_image_id], [product_id], [product_image_url]) values(@product_image_id, @product_id, @product_image_url)";

                                    using (SqlCommand sqlCommand = new SqlCommand(addBookCmd, con))
                                    {
                                        sqlCommand.Parameters.AddWithValue("@Id", productId);
                                        sqlCommand.Parameters.AddWithValue("@BookName", productName);
                                        sqlCommand.Parameters.AddWithValue("@BookPrice", price);
                                        sqlCommand.Parameters.AddWithValue("@BookWeight", productWeight);
                                        sqlCommand.Parameters.AddWithValue("@BookSupplier", supplier);
                                        sqlCommand.Parameters.AddWithValue("@BookDescription", productDesc);
                                        sqlCommand.Parameters.AddWithValue("@CategoryLevelOneId", categoryLevel1Id);
                                        sqlCommand.Parameters.AddWithValue("@CategoryLevelTwoId", categoryLevel2Id);
                                        sqlCommand.Parameters.AddWithValue("@Author", author);
                                        sqlCommand.Parameters.AddWithValue("@Size", size);
                                        sqlCommand.Parameters.AddWithValue("@Language", language);
                                        sqlCommand.Parameters.AddWithValue("@NumberOfPages", numberOfPages);
                                        sqlCommand.Parameters.AddWithValue("@CoverType", coverType);
                                        sqlCommand.Parameters.AddWithValue("@Age", age);
                                        sqlCommand.Parameters.AddWithValue("@PublicationYear", publicationYear);
                                        sqlCommand.Parameters.AddWithValue("@Translator", translator);

                                        sqlCommand.ExecuteNonQuery();
                                    }

                                    foreach (var imgUrl in imgUrls)
                                    {
                                        using (SqlCommand sqlCommandImage = new SqlCommand(addImageCmd, con))
                                        {
                                            Guid productImageId = Guid.NewGuid();
                                            sqlCommandImage.Parameters.AddWithValue("@product_image_id", productImageId);
                                            sqlCommandImage.Parameters.AddWithValue("@product_id", productId);
                                            sqlCommandImage.Parameters.AddWithValue("@product_image_url", imgUrl);

                                            sqlCommandImage.ExecuteNonQuery();
                                        }
                                    }

                                }
                            }
                            con.Close();
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
