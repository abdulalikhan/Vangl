using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using Discord.Rest;
using MerriamWebster.NET;
using Newtonsoft.Json.Linq;

namespace Vangl.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task Ping()
        {
            await ReplyAsync("pong");
        }

        [Command("help")]
        public async Task ShowHelp()
        {
            await ReplyAsync("!dict <word> -> defines <word>");
            await ReplyAsync("!libgen <Book Title> -> searches for <Book Title> on LibGen.Is");
            await ReplyAsync("!remindme <Description> <Time Interval in Seconds> -> sets a reminder");
            await ReplyAsync("!covidcases <Country> -> fetches COVID-19 Statistics for <Country>");
            await ReplyAsync("Use double quotes for arguments that contain spaces");
        }

        [Command("dict")]
        public async Task ShowMeaning(string word = null)
        {
            if (word == null)
            {
                await ReplyAsync("Please specify a word!");
                return;
            }
            string APIKey = Credentials.MerriamWebsterApiKey;
            string uri = "https://dictionaryapi.com/api/v3/references/collegiate/json/" + word + "?key=" + APIKey;
            var json = new WebClient().DownloadString(uri);
            var cc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            if (json.Length == 2)
            {
                await ReplyAsync("I don't know what that means");
                return;
            }
            JToken jt = cc[0].SelectToken("shortdef");
            if (jt == null)
            {
                await ReplyAsync("I don't know what that means");
                return;
            }
            string response = "";
            foreach (var def in cc[0].shortdef) {
                response = response + "\n" + Convert.ToString(def);
            }
            await ReplyAsync(response);
        }

        [Command("libgen")]
        public async Task QueryLibgen(string bookTitle = null)
        {
            if (bookTitle == null)
            {
                await ReplyAsync("Usage: !libgen " + '"' + "Book Title" + '"' + " (with quotes)");
                return;
            }
            string link = "http://libgen.is/search.php?&req=" + HttpUtility.UrlEncode(bookTitle);
            await ReplyAsync(link);
        }

        [Command("remindme")]
        public async Task RemindMe(string chore = null, [Remainder] int timeInterval = 0)
        {
            if (chore == null || timeInterval == 0)
            {
                await ReplyAsync("Usage: !remindme " + '"' + "Churn butter" + '"' + " <Time Interval in Seconds>");
                return;
            }
            if (timeInterval < 0)
            {
                await ReplyAsync("You know the rules, and so do I.");
                return;
            }
            _ = Task.Run(async () =>
              {
                  Thread.Sleep(timeInterval * 1000);
                  string thisUser = Context.Message.Author.Mention.ToString();
                  await ReplyAsync("[Reminder] " + thisUser + " : " + chore);
              });
        }

        [Command("covidcases")]
        public async Task GetConfirmedCases(string country = null)
        {
            if (country == null)
            {
                await ReplyAsync("Usage: !covidcase <Country>");
                return;
            }
            country.ToLower();
            country.Replace(' ', '-');
            string uri = "https://api.covid19api.com/country/" + country + "?from=2021-02-01T00:00:00Z";
            try
            {
                var json = new WebClient().DownloadString(uri);
                var cc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                if (json.Length == 0 || json == null)
                {
                    await ReplyAsync("Unable to fetch statistics");
                    return;
                }
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("COVID-19 Statistics for " + country);
                builder.AddField("Confirmed Cases", String.Format("{0:n0}", Convert.ToInt32(cc[cc.Count - 1].Confirmed)), true);
                builder.AddField("Deaths", String.Format("{0:n0}", Convert.ToInt32(cc[cc.Count - 1].Deaths)), true);
                builder.AddField("Recovered", String.Format("{0:n0}", Convert.ToInt32(cc[cc.Count - 1].Recovered)), true);
                builder.AddField("Active", String.Format("{0:n0}", Convert.ToInt32(cc[cc.Count - 1].Active)), true);
                builder.WithThumbnailUrl("https://i.ibb.co/1J06CyD/covid-19.png");
                builder.WithColor(Color.Red);
                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (WebException wex)
            {
                if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    await ReplyAsync("I can't find a country with that name");
                    return;
                }
            }
        }
    }
}
