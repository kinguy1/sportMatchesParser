using HtmlAgilityPack;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SiteParseTest
{
    class Program
    {
        const string FILE_PATH = @"c:\Matches.txt";

        static void Main(string[] args)
        {
            var buffer = new BufferBlock<Match>();

            var matches = FetchData(buffer);   //  Consumer
           
            ProduceAll(buffer).Wait(); //  Producers
            matches.Wait();

            string json = JsonConvert.SerializeObject(matches.Result.ToArray());

            WriteAsync(json).Wait();
        }

        /// <summary>
        /// Parsing new data from source
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        static async Task Parse(ITargetBlock<Match> target)
        {
            var url = @"https://futbolme.com/";

            HtmlWeb web = new HtmlWeb();

            var htmlDoc = web.Load(url);
            var competitionSelector = htmlDoc.DocumentNode.SelectNodes("//div[@class='col-xs-12 greenbox one-bordergrey-vert h40']");

            List<Competition> competitions = new List<Competition>();

            foreach (var item in competitionSelector)
            {
                var competition = item.SelectSingleNode(".//span[@class='tname visible-xs txt11']");

                competitions.Add(new Competition()
                {
                    Position = competition.StreamPosition,
                    Name = competition.InnerText
                });
            }

            var nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='boxpartido col-xs-12 whitebox nopadding']");

            foreach (HtmlNode node in nodes)
            {
                var name = node.SelectSingleNode(".//meta[@itemprop='name' and contains(@content,'-')]").GetAttributeValue("content", "");
                var teams = name.Split('-');

                //  Depands on the match status - live or upcoming
                var dateSelector = node.SelectSingleNode(".//div[@itemprop='startDate']");
                var dateSelector1 = node.SelectSingleNode(".//span[@class='text-center marco']");

                string dateStr = string.Empty;

                if (dateSelector != null)
                    dateStr = dateSelector.InnerText.Replace("\n", string.Empty).Trim();
                else if (dateSelector1 != null)
                    dateStr = dateSelector1.InnerText.Replace("\n", string.Empty).Trim();

                DateTime date;
                DateTime.TryParseExact(dateStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

                string competition = "";

                foreach (var item in competitions)
                {
                    if (item.Position < node.StreamPosition)
                        competition = item.Name;
                }

                Match match = new Match()
                {
                    Name = name,
                    TeamA = teams[0].Trim(),
                    TeamB = teams[1].Trim(),
                    Date = date,
                    Competition = competition
                };

                await target.SendAsync(match);
            }
        }

        /// <summary>
        /// Join both 2 producers - Parse and ReadAsync wait until both sources before saving to db
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        static async Task ProduceAll(ITargetBlock<Match> target)
        {
            var producer1 = Parse(target);
            var producer2 = ReadAsync(target);
            await Task.WhenAll(producer1, producer2);
            target.Complete();
        }

        /// <summary>
        /// Preparing the match list for saving do tb - include isExist check
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static async Task<List<Match>> FetchData(ISourceBlock<Match> source)
        {
            List<Match> matches = new List<Match>();

            while (await source.OutputAvailableAsync())
            {
                Match match = source.Receive();

                //  Check is match already exist in the db
                if (!matches.Any(m => m.Competition == match.Competition && m.TeamA == match.TeamA && m.TeamB == match.TeamB))
                    matches.Add(match);
            }

            return matches;
        }

        /// <summary>
        /// Write games to db (Json file)
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        static async Task WriteAsync(string json)
        {
            using (var sw = new StreamWriter(FILE_PATH))
            {
                await sw.WriteAsync(json);
            }
        }

        /// <summary>
        /// Read games list from db (Json file)
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        static async Task ReadAsync(ITargetBlock<Match> target)
        {
            if (!File.Exists(FILE_PATH))
                await WriteAsync(string.Empty);

            using (StreamReader r = new StreamReader(FILE_PATH))
            {
                string json = await r.ReadToEndAsync();
                List<Match> matches = JsonConvert.DeserializeObject<List<Match>>(json);

                if (matches != null)
                {
                    foreach (Match match in matches)
                    {
                        await target.SendAsync(match);
                    }
                }
            }
        }
       
    }
}
