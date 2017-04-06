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
using System.Threading;
using System.IO;

namespace myCustomStep
{
    public class myCustomStepSample : MediaButler.WorkflowStep.ICustomStepExecution
    {
        private static CloudMediaContext _MediaServicesContext = null;

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

        public void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            IJob job = (IJob)sender;

            if (e.PreviousState != e.CurrentState)
            {
                //PreviousJobState = e.CurrentState.ToString();
                Trace.TraceInformation("Job {0} state Changed from {1} to {2}", job.Name, e.PreviousState, e.CurrentState);
            }
        }

        private List<IAssetFile> GetFiles(IAsset myAsset, string filename)
        {
            List<IAssetFile> xLists = new List<IAssetFile>();
            foreach (var file in myAsset.AssetFiles)
            {
                if (file.Name.EndsWith(filename))
                {
                    xLists.Add(file);
                }
            }
            return xLists;
        }

        private void CopyAssetFiles(IAsset myAssetTo, List<IAssetFile> files)
        {
            foreach (var assetFile in files)
            {
                string magicName = assetFile.Name;
                assetFile.Download(magicName);
                try
                {
                    Trace.TraceInformation("Copying {0}", magicName);
                    IAssetFile newFile = myAssetTo.AssetFiles.Create(assetFile.Name);
                    newFile.Upload(magicName);
                    newFile.Update();
                }
                catch (Exception X)
                {
                    Trace.TraceError("Error CopyAssetFiles " + X.Message);
                    if (File.Exists(magicName))
                    {
                        System.IO.File.Delete(magicName);
                    }

                    throw X;
                }
                System.IO.File.Delete(magicName);
            }
            myAssetTo.Update();
        }

        public bool execute(ICustomRequest request)
        {
            bool response = true;
            MediaServicesCredentials xIdentity = new MediaServicesCredentials(request.MediaAccountName, request.MediaAccountKey);

            _MediaServicesContext = new CloudMediaContext(xIdentity);

            IJob job = _MediaServicesContext.Jobs.Create("Video Thumbnail Job");

            string MediaProcessorName = "Azure Media Video Thumbnails";

            var processor = GetLatestMediaProcessorByName(MediaProcessorName);

            IAsset curretAsset = _MediaServicesContext.Assets.Where(a => a.Id == request.AssetId).FirstOrDefault();

            IAsset video360 = _MediaServicesContext.Assets.Create(curretAsset.Name.ToString() + " 360", AssetCreationOptions.None);

            List<IAssetFile> filesToCopy;

            filesToCopy = GetFiles(curretAsset, "360_500.mp4");

            CopyAssetFiles(video360, filesToCopy);

            String configuration = "{\"version\":\"1.0\",\"options\":{\"outputAudio\" : \"false\", \"maxMotionThumbnailDurationInSecs\": \"10\", \"fadeInFadeOut\" : \"false\" }}";

            // Create a task with the encoding details, using a string preset.
            ITask task = job.Tasks.AddNew("My Video Thumbnail Task " + curretAsset.Id.ToString(),
                processor,
                configuration,
                TaskOptions.None);

            // Specify the input asset.
            task.InputAssets.Add(video360);

            // Specify the output asset.

            task.OutputAssets.AddNew(curretAsset.Id.ToString() + " Summarized", AssetCreationOptions.None);

            // Use the following event handler to check job progress.  
            job.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);

            // Launch the job.
            job.Submit();

            // Check job execution and wait for job to finish.
            Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);

            progressJobTask.Wait();

            // If job state is Error, the event handling
            // method for job progress should log errors.  Here we check
            // for error state and exit if needed.
            if (job.State == JobState.Error)
            {
                ErrorDetail error = job.Tasks.First().ErrorDetails.First();
                Console.WriteLine(string.Format("Error: {0}. {1}",
                                                error.Code,
                                                error.Message));
                response = false;
            }

            IAsset summarizedAsset = _MediaServicesContext.Assets.Where(a => a.Name == curretAsset.Id.ToString() + " Summarized").FirstOrDefault();

            List<IAssetFile> filesToCopy2;

            filesToCopy2 = GetFiles(summarizedAsset, ".mp4");

            CopyAssetFiles(curretAsset, filesToCopy2);

            video360.Delete();
            summarizedAsset.Delete();

            return response;
        }
        
        static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = _MediaServicesContext.MediaProcessors
                .Where(p => p.Name == mediaProcessorName)
                .ToList()
                .OrderBy(p => new Version(p.Version))
                .LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor",
                                                           mediaProcessorName));

            return processor;

        }
    }
}
