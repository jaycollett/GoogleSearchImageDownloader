using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoogleSearchImageDownloader
{
    class Program
    {
        private static HttpClient myHTTPClient; // never use USING with httpclient, this should be application life scope
        private const string baseGoogleImageSearchURL = "https://www.google.com/search?source=lnms&tbm=isch&sa=X&q=";
        private static string pathToSaveFiles;
        private static string imagePrefix;
        private static string imageSearchTerm;
        static void Main(string[] args)
        {
            if(args.Length < 3)
            {
                Console.WriteLine("Please provide three arguments, the first is the path to save images to, the second is the google search term and the last is the image prefix.");
                return;
            }

            try
            {
                pathToSaveFiles = args[0];
                imageSearchTerm = args[1];
                imagePrefix = args[2];

                HttpClientHandler httpHandler = new HttpClientHandler();
                // code to disable ssl validation (needed only for dev/staging)
                httpHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                httpHandler.ServerCertificateCustomValidationCallback = (APIHTTPClient, cert, cetChain, policyErrors) =>
                {
                    return true;
                };

                myHTTPClient = new HttpClient(httpHandler);

                // headers
                myHTTPClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux i686; rv:64.0) Gecko/20100101 Firefox/64.0");

                List<string> urls = GetUrls(GetHtmlCode(imageSearchTerm));
                int t = 0;
                Task[] tasks = new Task[urls.Count];
                foreach (string tmpString in urls)
                {
                    ImageObject tmpImgObj = new ImageObject { ImageNumber = t, ImageURL = tmpString };
                    tasks[t] = Task.Run(() => DownloadImage(tmpImgObj));
                    t++;
                }

                Task.WaitAll(tasks);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void DownloadImage(ImageObject tmpImgObj)
        {
            byte[] image = GetImage(tmpImgObj.ImageURL);
            if (image != null)
            {
                File.WriteAllBytes(pathToSaveFiles + $"\\{imagePrefix}_glImage_{tmpImgObj.ImageNumber}.jpg", image);
            }
            Console.WriteLine($"Downloaded image {tmpImgObj.ImageNumber}...");
        }
        private static string GetHtmlCode(string searchString)
        {
            try
            {
                myHTTPClient.DefaultRequestHeaders.Accept.Clear();
                myHTTPClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
                myHTTPClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
                myHTTPClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

                var googleImageQueryHttpResponse = myHTTPClient.GetAsync(new Uri(baseGoogleImageSearchURL + searchString)).Result;
                return googleImageQueryHttpResponse.Content.ReadAsStringAsync().Result;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return "";
        }

        private static List<string> GetUrls(string html)
        {
            var urls = new List<string>();

            int ndx = html.IndexOf("\"ou\"", StringComparison.Ordinal);

            while (ndx >= 0)
            {
                ndx = html.IndexOf("\"", ndx + 4, StringComparison.Ordinal);
                ndx++;
                int ndx2 = html.IndexOf("\"", ndx, StringComparison.Ordinal);
                string url = html.Substring(ndx, ndx2 - ndx);
                urls.Add(url);
                ndx = html.IndexOf("\"ou\"", ndx2, StringComparison.Ordinal);
            }
            return urls;
        }

        private static byte[] GetImage(string url)
        {
            try
            {
                var googleImageQueryHttpResponse = myHTTPClient.GetAsync(new Uri(url)).Result;
                if (googleImageQueryHttpResponse.IsSuccessStatusCode)
                    return googleImageQueryHttpResponse.Content.ReadAsByteArrayAsync().Result;
                else
                    return null;

            } catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }
    }

    public class ImageObject
    {
        public string ImageURL { get; set; }
        public int ImageNumber { get; set; }
    }
}
