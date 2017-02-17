using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using myCustomStep;
using MediaButler.WorkflowStep;

namespace myCustomStepTest
{
    [TestClass]
    public class myCustomStepTest
    {
        /// <summary>
        /// Test sample step, connect to AMS load Asset and looking for 
        /// JSON asset file, select or create a Locator and 
        /// make a http get callback with the asset file URL
        /// </summary>
        [TestMethod]
        public void TestMyCustomStepSample()
        {
            myCustomStepSample step = new myCustomStepSample();

            ICustomRequest request = new customeRequest();
            request.AssetId = System.Configuration.ConfigurationManager.AppSettings["AssetId"];
            request.MediaAccountName = System.Configuration.ConfigurationManager.AppSettings["MediaAccountName"];
            request.MediaAccountKey = System.Configuration.ConfigurationManager.AppSettings["MediaAccountKey"];

            Assert.AreEqual( step.execute(request),true);
        }
    }
}
