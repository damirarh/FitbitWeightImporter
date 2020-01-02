using CsvHelper;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FitbitWeightImporter
{
    class Program
    {
        private static readonly decimal height = 1.76m;
        private static readonly string csvFilePath = @"c:\Users\Damir\Dropbox\Work\Lists\weight.csv";
        private static readonly DateTime startDate = new DateTime(2019, 6, 13);
        private static readonly string url = "https://web-api.fitbit.com/1.1/user/-/body/log/weight.json";
        private static readonly string authorizationHeader = "Bearer xxx";

        static async Task Main(string[] args)
        {
            using (var reader = new StreamReader(csvFilePath))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Configuration.Delimiter = ";";
                    csv.Configuration.CultureInfo = CultureInfo.InvariantCulture;
                    var weightEntries = csv.GetRecords<WeightEntry>().ToArray();
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                        httpClient.DefaultRequestHeaders.Add("accept-language", "x_machine");
                        httpClient.DefaultRequestHeaders.Add("accept-locale", "en_EU");
                        httpClient.DefaultRequestHeaders.Add("authorization", authorizationHeader);
                        foreach (var weightEntry in weightEntries)
                        {
                            if (weightEntry.Date < startDate)
                            {
                                var response = await httpClient.PostAsync(url, GeneratePostContent(weightEntry.Date, weightEntry.Weight));
                                dynamic json = JObject.Parse(await response.Content.ReadAsStringAsync());
                                if (json.weightLog == null)
                                {
                                    throw new Exception();
                                }
                            }
                        }
                    }
                }
            }
        }

        static MultipartFormDataContent GeneratePostContent(DateTime date, decimal weight)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(CalculateBmi(weight).ToString("0.00", CultureInfo.InvariantCulture)), "bmi");
            content.Add(new StringContent(date.ToString("yyyy-MM-dd")), "date");
            content.Add(new StringContent(string.Empty), "fat");
            content.Add(new StringContent(CalculateUnixTimeMilliseconds(date).ToString()), "logId");
            content.Add(new StringContent("23:59:59"), "time");
            content.Add(new StringContent((weight * 1000m).ToString("0")), "weight");
            content.Add(new StringContent("API"), "source");
            return content;
        }

        static decimal CalculateBmi(decimal weight)
        {
            return weight / height / height;
        }

        static long CalculateUnixTimeMilliseconds(DateTime date)
        {
            var utcDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            return new DateTimeOffset(utcDate).ToUnixTimeMilliseconds();
        }
    }
}


