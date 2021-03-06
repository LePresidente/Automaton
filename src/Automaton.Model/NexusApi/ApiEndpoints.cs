﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autofac;
using Automaton.Model.Install;
using Automaton.Model.Interfaces;
using Automaton.Model.NexusApi.Interfaces;
using Newtonsoft.Json.Linq;

namespace Automaton.Model.NexusApi
{
    public class ApiEndpoints : IApiEndpoints
    {
        private readonly IApiBase _apiBase;
        private readonly ILogger _logger;

        private readonly HttpClient _baseHttpClient;

        public ApiEndpoints(IComponentContext components)
        {
            _apiBase = components.Resolve<IApiBase>();
            _logger = components.Resolve<ILogger>();

            _baseHttpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.nexusmods.com"),
                Timeout = TimeSpan.FromSeconds(10),
            };

            var platformType = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var headerString = $"Automaton/{Assembly.GetEntryAssembly().GetName().Version} ({Environment.OSVersion.VersionString}; {platformType}) {RuntimeInformation.FrameworkDescription}";

            _baseHttpClient.DefaultRequestHeaders.Add("User-Agent", headerString);
            _baseHttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _baseHttpClient.DefaultRequestHeaders.Add("Application-Name", "Automaton");
            _baseHttpClient.DefaultRequestHeaders.Add("Application-Version", $"{Assembly.GetEntryAssembly().GetName().Version}");
        }

        public async Task<string> GenerateModDownloadLinkAsync(string gameName, string modId, string fileId)
        {
            _logger.WriteLine($"GenerateModDownloadLink({gameName}, {modId}, {fileId})");

            var url = $"/v1/games/{gameName}/mods/{modId}/files/{fileId}/download_link";

            var apiResult = await MakeGenericApiCall(url);

            return JArray.Parse(apiResult)[0]["URI"].ToString();
        }

        public async Task<string> GenerateModDownloadLinkAsync(PipedData pipedData)
        {
            _logger.WriteLine($"GenerateModDownloadLink()");

            var url = $"/v1/games/{pipedData.Game}/mods/{pipedData.ModId}/files/{pipedData.FileId}/download_link" +
                      pipedData.AuthenticationParams;
            var apiResult = await MakeGenericApiCall(url);

            return JArray.Parse(apiResult)[0]["URI"].ToString();
        }

        public async Task<string> GenerateModDownloadLinkAsync(ExtendedMod mod)
        {
            _logger.WriteLine($"GenerateModDownloadLink()");

            if (mod.ModId == null || mod.FileId == null)
            {
                _logger.WriteLine($"Generated modlist failed. Invalid modID and/or modID");

                return null;
            }

            var url = $"/v1/games/{mod.TargetGame.ToLower()}/mods/{mod.ModId}/files/{mod.FileId}/download_link";
            var apiResult = await MakeGenericApiCall(url);

            if (apiResult == null)
            {
                _logger.WriteLine($"Failed to generate download link: {mod.ModName},{mod.ModId},{mod.FileId}");
                return null;
            }

            return JArray.Parse(apiResult)[0]["URI"].ToString();
        }

        private async Task<string> MakeGenericApiCall(string url)
        {
            if (_apiBase.ApiKey == string.Empty)
            {
                _logger.WriteLine("Invalid API key");
                // Throw exception here.
                return null;
            }

            try
            {
                if (!_baseHttpClient.DefaultRequestHeaders.Contains("APIKEY"))
                {
                    _baseHttpClient.DefaultRequestHeaders.Add("APIKEY", _apiBase.ApiKey);
                }

                if (_apiBase.RemainingDailyRequests == 0)
                {
                    _logger.WriteLine("User is out of API requests. This is no bueno.");

                    throw new Exception("You are out of API requests for today. This is not set by me, but the Nexus instead. You should never see this.");
                }

                var response = await _baseHttpClient.GetAsync(url);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.WriteLine($"API call does not equal OK. {url}");
                    return null;
                }

                _apiBase.RemainingDailyRequests =
                    Convert.ToInt32(response.Headers.GetValues("X-RL-Daily-Remaining").ToList().First());

                return await response.Content.ReadAsStringAsync();
            }

            catch (Exception e)
            {
                _logger.WriteLine($"Generic API request to URL: {url} has failed. {e.Message}");
                throw;
            }
        }
    }
}
