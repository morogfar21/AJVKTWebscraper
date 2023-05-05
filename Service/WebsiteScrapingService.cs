using CsvHelper;
using HtmlAgilityPack;
using Webscraper.AJVKT.API.Model;
using Google.Cloud.Translation.V2;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;

namespace Webscraper.AJVKT.API.Service
{
    public class WebsiteScrapingService : IWebsiteScrapingService
    {
        private readonly ILogger<WebsiteScrapingService> _logger;
        public WebsiteScrapingService(ILogger<WebsiteScrapingService> logger)
        {
            _logger = logger;
        }

        public async Task ScrapeWebsite()
        {
            var httpClient = new HttpClient();
            //var url = "https://www.levior.cz/festa?products-list-onpage=72";
            //var response = await httpClient.GetAsync(url);
            //var responseBody = await response.Content.ReadAsStringAsync();
            //// Parse the HTML using HtmlAgilityPack
            //var htmlDocument = new HtmlDocument();
            //htmlDocument.LoadHtml(responseBody);


            // Initialize the translation client ** IMPORTANT ** RUN "gcloud auth application-default login" in cmd
            //var credential_path = @"C:\Users\cendi\AppData\Roaming\gcloud\application_default_credentials.json";
            //System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credential_path);

            //var translationClient = TranslationClient.Create();
            // Language support Overview - https://cloud.google.com/translate/docs/languages

            var products = new List<Product>();

            //var allHtmlPagesList = GetSourceInformationFromAllProductPagesAsync(); // list of html pages as html documents
            //Foreach htmldocument in list, select node product item, forach productnode, select product values.
            var htmlDocumentsList = GetWebsiteSourceInformation(); // Login process enabling prices on the website.
            foreach(var htmlDocument in htmlDocumentsList)
            {
                // Each product on the pages
                var productNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='product__item product__item--list col-12 col-sm-6 col-md-4 col-lg-4 col-xl-3 col-xxl-2']");
                
                //Each value in each product
                foreach (var productNode in productNodes)
                {
                    var productTitleNode = productNode.SelectSingleNode(".//h3[@class='product__title clip-text clip-text--3']");
                    //var productTitle = await TranslateTextAsync(translationClient, productTitleNode.InnerText.Trim(), "cs", "da");
                    var productTitle = productTitleNode.InnerText.Trim();

                    var productCodeNode = productNode.SelectSingleNode(".//span[@class='product__code d-none d-sm-block']");
                    //var productCode = await TranslateTextAsync(translationClient, productCodeNode?.InnerText?.Trim() ?? "", "cs", "da");
                    var productCode = productCodeNode.InnerText.Trim();

                    var productPriceNode = productNode.SelectSingleNode(".//div[@class='product__price d-none d-sm-block']");
                    //var productPrice = await TranslateTextAsync(translationClient, productPriceNode?.InnerText?.Trim() ?? "", "cs", "da");
                    var productPrice = productPriceNode.InnerText.Trim().Replace("&nbsp", "euro");

                    var productImageNode = productNode.SelectSingleNode(".//img[@class='img--responsive img--contain margin-center']");
                    var productImageSrc = productImageNode?.GetAttributeValue("src", "") ?? "";

                    //var productQuantityNode = productNode.SelectSingleNode(".//div[@class='product__quantity-block']");
                    //var productQuantity = await TranslateTextAsync(translationClient, productQuantityNode?.InnerText?.Trim() ?? "", "cs", "da");
                    //var productQuantity = productQuantityNode.InnerText.Trim();

                    //var productQuantityNodeBlock = productNode.SelectNodes(".//div[@class='product-amount__text']");
                    var productQuantityNodeBlock = productNode.SelectSingleNode(".//div[@class='product-amount__text']");
                    var productAmount = productQuantityNodeBlock.InnerText.Trim();
                    //var productAmount = "";
                    //foreach (var quantity in productQuantityNodeBlock)
                    //{
                    //    productAmount = string.Join(" - ", quantity.InnerText.Trim());
                    //}

                    if (!string.IsNullOrEmpty(productImageSrc))
                    {
                        // remove invalid syntax after .jpg file type - example: 26986.jpg?1517901840'
                        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(productImageSrc);
                        var imageFileName = filenameWithoutExtension + ".jpg";
                        var imageFilePath = Path.Combine("E:\\temp\\images", imageFileName);

                        if (!File.Exists(imageFilePath))
                        {
                            try
                            {
                                //var imageBytes = await httpClient.GetByteArrayAsync("https://www.levior.cz" + productImageSrc);
                                var result = await httpClient.GetAsync("https://www.levior.cz" + productImageSrc);
                                if(result.IsSuccessStatusCode)
                                {
                                    var imageBytes = await result.Content.ReadAsByteArrayAsync();
                                    if (imageBytes != null)
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(imageFilePath));
                                        File.WriteAllBytes(imageFilePath, imageBytes);
                                    }
                                }
                            }
                            catch(Exception e)
                            {
                                _logger.LogInformation($"Exception occured: {e}");
                            }
                        }

                        products.Add(new Product { Title = productTitle, Code = productCode, Price = productPrice, Quantity = productQuantity, ImageFilePath = imageFileName, Amounts = productAmount });

                        // Write the product to the CSV file
                        var csvFilePath = Path.Combine("E:\\temp", "products.csv");
                        using (var writer = new StreamWriter(csvFilePath, append: true))
                        using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecord(new Product { Title = productTitle, Code = productCode, Price = productPrice, Quantity = productQuantity, ImageFilePath = imageFileName, Amounts = productAmount });
                        }
                    }
                    else
                    {
                        products.Add(new Product { Title = productTitle, Code = productCode, Price = productPrice, Quantity = productQuantity, ImageFilePath = "", Amounts = productAmount});
                    }
                }
            }

           
            _logger.LogInformation("Finished writing csv file");
        }

        static async Task<string> TranslateTextAsync(TranslationClient client, string text, string sourceLanguage, string targetLanguage)
        {
            var response = await client.TranslateTextAsync(text, targetLanguage, sourceLanguage);
            return response.TranslatedText;
        }

        private List<HtmlDocument> GetWebsiteSourceInformation()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("--disable-gpu");
            chromeOptions.AddArguments("--headless");

            using (var driver = new ChromeDriver(chromeOptions))
            {
                // Navigate to the login page
                driver.Navigate().GoToUrl("https://www.levior.cz/en/prihlaseni");

                // Find the username and password fields and enter your credentials
                var usernameField = driver.FindElement(By.Name("login"));
                usernameField.SendKeys("mail@ajvkt.dk");
                var passwordField = driver.FindElement(By.Name("password"));
                passwordField.SendKeys("Mgwtpatt2023");

                // Find the login button and click it
                var loginButton = driver.FindElement(By.ClassName("btn"));
                loginButton.Click();

                // Wait for the page to load
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                // Navigate to the page containing the information you want to scrape
                driver.Navigate().GoToUrl("https://www.levior.cz/en/festa?products-list-onpage=72&fbclid=IwAR2yTwrjwtkzQFGWFmvYtk25ShxQXWY69qm6Rh8Ag4InvPWSp_9FU8Tbi4g");

                // Get the Mainpage source
                var pageSource = driver.PageSource;

                // Parse the HTML using HtmlAgilityPack
                var htmlDocuments = new List<HtmlDocument>();
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(pageSource);
                //Add main page to html list.
                htmlDocuments.Add(htmlDocument);

                // Loop through all 34 documents and add them to the list
                for (int i = 2; i <= 34; i++)
                {
                    // Send an HTTP GET request to the website and retrieve the response
                    driver.Navigate().GoToUrl($"https://www.levior.cz/en/festa?products-list-paging-p={i}&products-list-onpage=72");

                    // Get the Mainpage source
                    var pageSourceHtml = driver.PageSource;

                    // Create a new HTML document and load the content from the string
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(pageSourceHtml);

                    // Add the document to the list
                    htmlDocuments.Add(doc);
                }

                return htmlDocuments;
            }
        }
    }
}
