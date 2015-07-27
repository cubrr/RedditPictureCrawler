// RedditPictureCrawler - Crawls subreddit for pictures.
// Copyright © 2015 Joona Heikkilä
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cubrr
{
    class RedditPictureCrawler
    {
        const string SubredditEndpoint = "http://www.reddit.com/r/pics/hot.json";
        const string SaveFolderPath = "/tmp/pics/";

        public static void Main(string[] args)
        {
            ListingData listingData = GetListing(SubredditEndpoint).Data;
            var downloadTasks = new List<Task>();
            foreach (Container<LinkData> linkData in listingData.Children)
            {
                string folderName = Path.Combine(SaveFolderPath, linkData.Data.Author);
                if (Path.HasExtension(linkData.Data.Url.ToString()))
                {
                    var fileName = Path.GetFileName(linkData.Data.Url.ToString());
                    var savePath = Path.Combine(folderName, fileName);
                    Directory.CreateDirectory(folderName);
                    downloadTasks.Add(SaveImage(linkData.Data.Url, savePath));
                }
            }
            Task.WhenAll(downloadTasks).Wait();
            Console.WriteLine("All complete!");
            Console.ReadKey();
        }

        public static Container<ListingData> GetListing(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
                // StreamReader closes the underlying stream
            {
                string readResponse = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Container<ListingData>>(readResponse);
            }
        }

        public static async Task SaveImage(Uri downloadPath, string saveDirectory)
        {
            if (File.Exists(saveDirectory))
            {
                Console.WriteLine("File already downloaded: " + saveDirectory);
                return;
            }
            var client = new HttpClient();
            using (Stream input = await client.GetStreamAsync(downloadPath))
            using (var saveStream = new FileStream(saveDirectory, FileMode.CreateNew))
            {
                await input.CopyToAsync(saveStream);
                Console.WriteLine("Finish " + downloadPath);
            }
        }
    }

    public class Container<T>
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class ListingData
    {
        [JsonProperty("modhash")]
        public string ModHash { get; set; }

        [JsonProperty("children")]
        public Container<LinkData>[] Children { get; set; }

        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty("before")]
        public string Before { get; set; }

        public Container<LinkData> this[int i]
        {
            get { return Children[i]; }
        }
    }

    public class LinkData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}
