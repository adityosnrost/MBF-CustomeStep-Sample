using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaButler.WorkflowStep;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;

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

        private async Task httpCallBack(string url, string argument)
        {
            argument=System.Web.HttpUtility.UrlEncode(argument);
            string fullCall = string.Format("{0}{1}", url, argument);
            using (var client = new HttpClient())
            {

                var r = await client.GetAsync(url);
                string result = await r.Content.ReadAsStringAsync();
                Trace.TraceInformation("myCustomStepSample response {0}", result);

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

                string jsonUrl = locator.Path + "/" + jsonFile.Name;
                string callbackUrl = "https://www.google.cl/#q=";

                httpCallBack(callbackUrl, jsonUrl).Wait();

                response = true;
            }
            return response;    
        }
    }
}
