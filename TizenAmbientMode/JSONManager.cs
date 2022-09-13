using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TizenAmbientMode
{
    class JSONManager
    {
        public ImageDatabase imgDatabase { get; }
        HttpClient httpclient;
        String ErrorLog;

        public JSONManager()
        {
            httpclient = new HttpClient();
            imgDatabase = new ImageDatabase();
        }


        /// <summary>
        /// Starts the process to retrieve chromecast images from chromecast homepage
        /// URL: https://clients3.google.com/cast/chromecast/home/
        /// </summary>
        public void ChromecastBackgrounds()
        {
            const string ChromeCastURL = "https://clients3.google.com/cast/chromecast/home/";
            string htmlDoc;

            try
            {
                using (HttpResponseMessage response = httpclient.GetAsync(ChromeCastURL).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        htmlDoc = content.ReadAsStringAsync().Result;
                    }
                }
                if (htmlDoc.Contains("class=\"XyDo4b\""))
                {
                    htmlDoc = htmlDoc.Substring(htmlDoc.IndexOf("class=\"XyDo4b\""));
                    htmlDoc = htmlDoc.Substring(htmlDoc.IndexOf("JSON.parse('") + "JSON.parse('".Length);
                    htmlDoc = htmlDoc.Substring(0, htmlDoc.IndexOf("\')"));
                }
                else if (htmlDoc.Contains("googleusercontent.com"))
                {
                    int link_approx = htmlDoc.IndexOf("googleusercontent.com") - 45;
                    // Finding the last JSON.parse closest to the first GoogleUsercontent.com link
                    htmlDoc = htmlDoc.Substring(htmlDoc.IndexOf("JSON.parse('", link_approx));
                    htmlDoc = htmlDoc.Substring(0, htmlDoc.IndexOf("\')"));

                }
                else throw new Exception("Default class nor the links were not found");

                htmlDoc = HexToUTF8FormatCorrection(htmlDoc);
                // Going three levels down the array of the links in Google's JSON-ish file
                // links are in the first element of a 2-element array. which itself is another array. but only the
                // first element has all the data. Lastly, the third-level element should be parsed into a ASCII file and 
                // the links will be extracted
                using (JsonDocument JTemp = JsonDocument.Parse(htmlDoc))
                {
                    var ArEnumerator = JTemp.RootElement.EnumerateArray();
                    ArEnumerator.MoveNext();
                    JsonElement FirstLevel = ArEnumerator.Current;
                    ArEnumerator = FirstLevel.EnumerateArray();
                    foreach (var element in ArEnumerator)
                    {
                        var SecondLevel = element.EnumerateArray();
                        SecondLevel.MoveNext();
                        var s = imgDatabase.AddLink(DecodeEncodedNonAsciiCharacters(SecondLevel.Current.ToString()), "Google", false);
                    }

                }
            }
            catch (Exception e)
            {
                ErrorLog += e.Message + Environment.NewLine;
            }

        }
        /// <summary>
        /// Starts the process to retrieve the most recent Bing images
        /// </summary>
        public void BingBackgrounds()
        {
            const string bingURL = "http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=8&mkt=en-US";
            string htmlDoc;

            try
            {
                using (HttpResponseMessage response = httpclient.GetAsync(bingURL).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        htmlDoc = content.ReadAsStringAsync().Result;
                    }
                }
                using (JsonDocument JDoc = JsonDocument.Parse(htmlDoc))
                {
                    JsonElement imagesArray;
                    if (JDoc.RootElement.TryGetProperty("images", out imagesArray))
                    {
                        foreach (var item in imagesArray.EnumerateArray())
                        {
                            imgDatabase.AddLink("www.bing.com" + item.GetProperty("url").ToString(), "Bing");
                        }
                    }
                    else
                    {
                        throw new Exception("Images not found in Bing JSON file");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog += e.Message + Environment.NewLine;
            }

        }

        /// <summary>
        /// Receives a string input containing Non-ASCII characters ( like \u003d ) and returns
        /// the equivalent ASCII character
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }

        /// <summary>
        /// Re-formats the string into normal UTF8 from HEX format ( '\x5b' ==> '[' )
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string HexToUTF8FormatCorrection(string input)
        {
            String inp = input;
            inp = inp.Replace("\\x22", "\"");
            inp = inp.Replace("\\x5b", "[");
            inp = inp.Replace("\\x5d", "]");
            inp = inp.Replace("\\/", "/");
            inp = inp.Replace("\\x27", "'");
            inp = inp.Replace("\\x7b", "{");
            inp = inp.Replace("\\x7d", "}");
            inp = inp.Replace("\\x7e", "~");
            return inp;

        }
    }

    /// <summary>
    /// Class that stores and handles Image links while the program is running
    /// </summary>
    class ImageDatabase
    {
        private List<ImageLink> Links;
        public int currentIndex;
        public ImageDatabase()
        {
            Links = new List<ImageLink>();
            currentIndex = 0;
        }

        public int AvailableImages()
        { return Links.Count; }
        public string CurrentImgURL()
        {
            try
            {
                if (Links.Count > 0)
                {
                    currentIndex %= Links.Count;
                    return Links[currentIndex].URL;
                }
                else throw new Exception("No Images Available yet");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        public void MoveNext(int step = 1)
        {
            if (Links.Count > 1)
            {
                currentIndex += step;
                currentIndex %= Links.Count;
            }
            else
            {
                currentIndex = 0;
            }
        }

        public void MoveNextRandom()
        { 
            var R = new Random(); 
            int idx = R.Next(0, Links.Count);
            while (idx==currentIndex)
            {
                idx = R.Next(0, Links.Count);
            }
            currentIndex = idx;
        }
        /// <summary>
        /// Adds a retrieved link to the image database for later reloading
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="Source">either Google or Bing for now</param>
        /// <param name="IgnoreDuplicate"></param>
        /// <returns></returns>
        public string AddLink(string URL, string Source, bool IgnoreDuplicate = false)
        {
            if (IgnoreDuplicate)
            {
                Links.Add(new ImageLink(URL, Source));
                return "Added an image without checking for duplicates";
            }
            else
            {
                if (!isThereDuplicateLink(URL))
                {
                    Links.Add(new ImageLink(URL, Source));
                    return "Added a new unique image";
                }
                else return "This image is already present in the database";
            }

        }


        /// <summary>
        /// checks a URL against the current database for duplicates.
        /// Returns True if there is a duplicate
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
        private bool isThereDuplicateLink(string URL)
        {
            foreach (var item in Links)
            {
                if (item.URL.Contains(URL))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Each imagelink has a url and a source (either Google or Bing for now)
    /// </summary>
    struct ImageLink
    {
        public string URL;
        public string source;
        public ImageLink(string url, string src)
        {
            URL = url;
            source = src;
        }
    }
}