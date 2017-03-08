using System;
using System.Collections.Generic;
using System.Linq;
using MediaButler.WorkflowStep;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace myCustomStep
{
    public class myCustomStepSample : MediaButler.WorkflowStep.ICustomStepExecution
    {
        private CloudMediaContext _MediaServicesContext;

        private ILocator CreateStreamingLocator(IAsset theAsset, int daysForWhichStreamingUrlIsActive)
        {
            ILocator locator = null;

            var accessPolicy = _MediaServicesContext.AccessPolicies.Create(
                theAsset.Name
                , TimeSpan.FromDays(daysForWhichStreamingUrlIsActive)
                , AccessPermissions.Read);

            locator = _MediaServicesContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, theAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));

            return locator;
        }

        private async Task httpCallBack(string url, string jsonUrl)
        {
            Dictionary<String, Object> values;
            using (var client = new HttpClient())
            {
                var moderateResponse = await client.GetAsync(jsonUrl);
                string moderateResult = await moderateResponse.Content.ReadAsStringAsync();
                values = JsonConvert.DeserializeObject<Dictionary<String, Object>>(moderateResult);

                var content = new StringContent(values["fragments"].ToString(), Encoding.UTF8, "application/json");
                var sendModerateResult = await client.PostAsync(url, content);
                Trace.TraceInformation("myCustomStepSample response {0}", sendModerateResult);
            }

        }

        public bool execute(ICustomRequest request)
        {
            bool response = false;
            MediaServicesCredentials xIdentity = new MediaServicesCredentials(request.MediaAccountName, request.MediaAccountKey);

            _MediaServicesContext = new CloudMediaContext(xIdentity);

            IAsset curretAsset = _MediaServicesContext.Assets.Where(a => a.Id == request.AssetId).FirstOrDefault();

            IAssetFile jsonFile = curretAsset.AssetFiles.Where(f => f.Name.EndsWith(".json")).FirstOrDefault();

            if (jsonFile != null)
            {

                ILocator locator = curretAsset.Locators.Where(l => l.Type == LocatorType.OnDemandOrigin).FirstOrDefault();
                if (locator == null)
                {
                    locator = CreateStreamingLocator(curretAsset, 365);
                }

                string jsonUrl = locator.Path + jsonFile.Name;
                string callbackUrl = "http://13.76.101.247:8010/httpcallback/ms2";

                httpCallBack(callbackUrl, jsonUrl).Wait();

                response = true;
            }
            return response;
        }
    }
}
