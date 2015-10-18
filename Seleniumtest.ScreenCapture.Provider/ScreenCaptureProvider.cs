using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using Microsoft.Expression.Encoder.ScreenCapture;
using Seleniumtest.Provider.Shared.Enum;
using Seleniumtest.Provider.Shared.Helpers;
using Seleniumtest.Provider.Shared.Providers;

namespace Seleniumtest.ScreenCapture.Provider
{
    public class ScreenCaptureProvider : IScreenCaptureProvider
    {
        private readonly IUrlHelper _urlHelper;
        private readonly IEmbeddedResourceHelper _embeddedResourceHelper;
        private readonly IAzureBlobStorage _azureBlobStorage;
        private ScreenCaptureJob ScreenCaptureJob { get; set; }
        private string OutputScreenCaptureFile { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenCaptureProvider"/> class.
        /// </summary>
        /// <param name="tempFolder">The temporary folder.</param>
        public ScreenCaptureProvider(string tempFolder): this(tempFolder, new UrlHelper(), new EmbeddedResourceHelper(), new AzureBlobStorage())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenCaptureProvider"/> class.
        /// </summary>
        /// <param name="tempFolder">The temporary folder.</param>
        /// <param name="urlHelper">The URL helper.</param>
        /// <param name="embeddedResourceHelper"></param>
        /// <param name="azureBlobStorage">The azure BLOB storage.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        public ScreenCaptureProvider(string tempFolder, IUrlHelper urlHelper, IEmbeddedResourceHelper embeddedResourceHelper, IAzureBlobStorage azureBlobStorage)
        {
            _urlHelper = urlHelper;
            _embeddedResourceHelper = embeddedResourceHelper;
            _azureBlobStorage = azureBlobStorage;

            ScreenCaptureJob = new ScreenCaptureJob();
            if (tempFolder == null || !Directory.Exists(tempFolder))
            {
                throw new DirectoryNotFoundException(string.Format("Directory {0} not found", tempFolder));
            }

            OutputScreenCaptureFile = string.Format("{0}\\ScreenCapture.wmv", tempFolder);
            ScreenCaptureJob.OutputScreenCaptureFileName = OutputScreenCaptureFile;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            ScreenCaptureJob.Start();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            ScreenCaptureJob.Stop();
        }

        /// <summary>
        /// Saves the specified page source.
        /// </summary>
        /// <param name="pageSource">The page source.</param>
        /// <param name="url">The URL.</param>
        /// <param name="message">The message.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="eventType">Type of the event.</param>
        public void Save(string pageSource, string url, string message, string methodName, EventType eventType)
        {
            this.Stop();
            
            //Save the video file in the blobstorage container
            if (File.Exists(OutputScreenCaptureFile))
            {
                string eventTypeName = eventType == EventType.Info ? "info" : "error";

                string environmentUrl = _urlHelper.GetEnvironmentName(url);

                DateTime dateTime = DateTime.Now;
                string videoFileName = Path.GetFileName(OutputScreenCaptureFile);
                string blobVideoName = string.Format("seleniumscreencaptures/{0}/{1}/{2}/{3}{4}/{5}/{6}", dateTime.Year, dateTime.Month, dateTime.Day, environmentUrl, methodName, eventType, videoFileName);

                byte[] fileByteArray = File.ReadAllBytes(OutputScreenCaptureFile);

                _azureBlobStorage.Save("seleniumscreencaptures", fileByteArray, blobVideoName, "video/x-ms-wmv");

                File.Delete(OutputScreenCaptureFile);

                //Create the html template
                string videoEmbedCode = GenerateEmbedVideoCode(blobVideoName);
                var htmlFile = CreateScreenCaptureErrorTemplate(videoEmbedCode, pageSource, url, message, methodName);

                byte[] htmlTemplateByteArray = Encoding.ASCII.GetBytes(htmlFile);
                string fileName = string.Format("{0}.html", DateTime.Now.ToString("HH-mm-ss"));

                
                string blobFileName = string.Format("seleniumscreenshots/{0}/{1}/{2}/{3}{4}/{5}/{6}", dateTime.Year, dateTime.Month, dateTime.Day, environmentUrl, methodName, eventTypeName, fileName);
                _azureBlobStorage.Save("seleniumscreenshots", htmlTemplateByteArray, blobFileName, "text/html");
            }
        }

        private string GenerateEmbedVideoCode(string blobFileName)
        {
            const string htmlTemplateFileName = "EmbedVideo.html";
            string html = _embeddedResourceHelper.LoadTemplate(Assembly.GetExecutingAssembly(), htmlTemplateFileName);

            return html.Replace("{embeddedvideo}", "/mediafile?path=" + blobFileName);
        }

        private string CreateScreenCaptureErrorTemplate(string embeddedVideo, string pageSource, string url, string message, string methodName)
        {
            const string htmlTemplateFileName = "ScreenCaptureTemplate.html";
            string html = _embeddedResourceHelper.LoadTemplate(Assembly.GetExecutingAssembly(), htmlTemplateFileName);
            var encodedPageSource = HttpUtility.HtmlEncode(pageSource);
            return html.Replace("{url}", url).Replace("{message}", message).Replace("{embeddedvideo}", embeddedVideo).Replace("{pagesource}", encodedPageSource).Replace("{methodName}", methodName);
        }
    }
}
