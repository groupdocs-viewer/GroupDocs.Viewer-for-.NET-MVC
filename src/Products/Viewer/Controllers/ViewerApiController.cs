using GroupDocs.Viewer.Exceptions;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using GroupDocs.Viewer.MVC.Products.Viewer.Config;
using GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Xml.Linq;
using WebGrease.Css.Extensions;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Controllers
{
    /// <summary>
    /// ViewerApiController.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ViewerApiController : ApiController
    {
        /// <summary>
        /// Map for locking keys in viewer cache.
        /// </summary>
        protected static readonly ConcurrentDictionary<string, object> KeyLockerMap = new ConcurrentDictionary<string, object>();

        private static readonly Common.Config.GlobalConfiguration globalConfiguration = new Common.Config.GlobalConfiguration();
        
        private readonly IInputHandler InputHandler;

        private readonly ICacheHandler CacheHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerApiController"/> class.
        /// </summary>
        public ViewerApiController()
        {
            List<string> fontsDirectory = new List<string>();
            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetFontsDirectory()))
            {
                fontsDirectory.Add(globalConfiguration.Viewer.GetFontsDirectory());
            }

            this.InputHandler = new LocalInputHandler(globalConfiguration);
            this.CacheHandler = new LocalCacheHandler(globalConfiguration);
        }

        /// <summary>
        /// Loads Viewer configuration.
        /// </summary>
        /// <returns>Viewer configuration.</returns>
        [HttpGet]
        [Route("loadConfig")]
        public ViewerConfiguration LoadConfig()
        {
            return globalConfiguration.Viewer;
        }

        /// <summary>
        /// Gets all files and directories from sample directory:
        /// src/DocumentSamples/Viewer/.
        /// </summary>
        /// <returns>List of files and directories.</returns>
        [HttpPost]
        [Route("loadFileTree")]
        public HttpResponseMessage GetFileTree()
        {
            try
            {
                List<FileDescriptionEntity> filesList = this.InputHandler.GetFilesList();

                return this.Request.CreateResponse(HttpStatusCode.OK, filesList);
            }
            catch (Exception ex)
            {
                return this.Request.CreateResponse(HttpStatusCode.OK, Resources.GenerateException(ex));
            }
        }

        /// <summary>
        /// Gets document pages data, dimensions and angles.
        /// </summary>
        /// <param name="postedData">Posted data with document guid.</param>
        /// <returns>Document pages data, dimensions and angles.</returns>
        [HttpPost]
        [Route("loadDocumentDescription")]
        public HttpResponseMessage GetDocumentData(PostedDataEntity postedData)
        {
            try
            {
                LoadDocumentEntity loadDocumentEntity = GetDocumentPages(postedData, globalConfiguration.Viewer.GetPreloadPageCount() == 0, this.InputHandler, this.CacheHandler);

                // return document description
                return this.Request.CreateResponse(HttpStatusCode.OK, loadDocumentEntity);
            }
            catch (PasswordRequiredException ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.Forbidden, Resources.GenerateException(ex, postedData.password));
            }
            catch (Exception ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.GenerateException(ex, postedData.password));
            }
        }

        /// <summary>
        /// Gets document page info.
        /// </summary>
        /// <param name="postedData">Posted data with page number.</param>
        /// <returns>Document page info.</returns>
        [HttpPost]
        [Route("loadDocumentPage")]
        public HttpResponseMessage GetDocumentPage(PostedDataEntity postedData)
        {
            string password = string.Empty;
            try
            {
                string documentGuid = postedData.guid;
                int pageNumber = postedData.page;
                password = string.IsNullOrEmpty(postedData.password) ? null : postedData.password;
                string fileCachePath = this.CacheHandler.GetFileCachePath(documentGuid);

                IViewerCache cache = new FileViewerCache(fileCachePath);

                PageDescriptionEntity page;
                if (globalConfiguration.Viewer.GetIsHtmlMode())
                {
                    using (HtmlViewer htmlViewer = new HtmlViewer(() => this.InputHandler.GetFileStream(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                    {
                        page = this.GetPageDescritpionEntity(htmlViewer, documentGuid, pageNumber, fileCachePath);
                    }
                }
                else
                {
                    using (PngViewer pngViewer = new PngViewer(() => this.InputHandler.GetFileStream(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                    {
                        page = this.GetPageDescritpionEntity(pngViewer, documentGuid, pageNumber, fileCachePath);
                    }
                }

                return this.Request.CreateResponse(HttpStatusCode.OK, page);
            }
            catch (Exception ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.Forbidden, Resources.GenerateException(ex, password));
            }
        }

        /// <summary>
        /// Rotates specific page(s) of the document.
        /// </summary>
        /// <param name="postedData">Document page number to rotate and rotation angle.</param>
        /// <returns>Rotated document page object.</returns>
        [HttpPost]
        [Route("rotateDocumentPages")]
        public HttpResponseMessage RotateDocumentPages(PostedDataEntity postedData)
        {
            try
            {
                var documentGuid = postedData.guid;
                var pageNumber = postedData.pages[0];
                string password = string.IsNullOrEmpty(postedData.password) ? null : postedData.password;
                string fileCachePath = this.CacheHandler.GetFileCachePath(documentGuid);

                // Delete page cache-files before regenerating with another angle.
                var cacheFiles = Directory.GetFiles(fileCachePath).Where(f => Path.GetFileName(f).StartsWith($"p{pageNumber}"));
                cacheFiles.ForEach(f => File.Delete(f));

                // Getting new rotation angle value.
                var currentAngle = GetCurrentAngle(pageNumber, Path.Combine(fileCachePath, "PagesInfo.xml"));
                int newAngle = GetNewAngleValue(currentAngle, postedData.angle);
                SaveChangedAngleInCache(fileCachePath, pageNumber, newAngle);

                IViewerCache cache = new FileViewerCache(fileCachePath);
                PageDescriptionEntity page;
                if (globalConfiguration.Viewer.GetIsHtmlMode())
                {
                    using (HtmlViewer htmlViewer = new HtmlViewer(() => this.InputHandler.GetFileStream(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password), pageNumber, newAngle))
                    {
                        page = this.GetPageDescritpionEntity(htmlViewer, documentGuid, pageNumber, fileCachePath);
                    }
                }
                else
                {
                    using (PngViewer pngViewer = new PngViewer(() => this.InputHandler.GetFileStream(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                    {
                        page = this.GetPageDescritpionEntity(pngViewer, documentGuid, pageNumber, fileCachePath);
                    }
                }

                return this.Request.CreateResponse(HttpStatusCode.OK, page);
            }
            catch (Exception ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.GenerateException(ex));
            }
        }

        /// <summary>
        /// Downloads curerntly viewed document.
        /// </summary>
        /// <param name="path">Path of the document to download.</param>
        /// <returns>Document stream as attachement.</returns>
        [HttpGet]
        [Route("downloadDocument")]
        public HttpResponseMessage DownloadDocument(string path)
        {
            return this.InputHandler.DownloadFile(path);
        }

        /// <summary>
        /// Downloads requested resource file.
        /// </summary>
        /// <param name="guid">Name of the file containing resource.</param>
        /// <param name="resourceName">Name of the resource file.</param>
        /// <returns>Document stream as attachement.</returns>
        [HttpGet]
        [Route("resources/{guid}/{resourceName}")]
        public HttpResponseMessage GetResource(string guid, string resourceName)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                var path = Path.Combine(globalConfiguration.Viewer.GetFilesDirectory(), globalConfiguration.Viewer.GetCacheFolderName(), guid, resourceName);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                response.Content = new StreamContent(fileStream);
                var fileName = Path.GetFileName(path);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileName));
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline");
                response.Content.Headers.ContentDisposition.FileName = fileName;
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Uploads document.
        /// </summary>
        /// <returns>Uploaded document object.</returns>
        [HttpPost]
        [Route("uploadDocument")]
        public HttpResponseMessage UploadDocument()
        {
            try
            {
                UploadedDocumentEntity uploadedDocument = this.InputHandler.UploadFile();

                return this.Request.CreateResponse(HttpStatusCode.OK, uploadedDocument);
            }
            catch (Exception ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.GenerateException(ex));
            }
        }

        /// <summary>
        /// Loads document pages thumbnails.
        /// </summary>
        /// <param name="loadDocumentRequest">Posted data with document guid.</param>
        /// <returns>Data of all document pages.</returns>
        [HttpPost]
        [Route("loadThumbnails")]
        public LoadDocumentEntity GetPagesThumbnails(PostedDataEntity loadDocumentRequest)
        {
            return GetDocumentPages(loadDocumentRequest, true, this.InputHandler, this.CacheHandler);
        }

        /// <summary>
        /// Loads print version.
        /// </summary>
        /// <param name="loadDocumentRequest">PostedDataEntity.</param>
        /// <returns>Data of all document pages.</returns>
        [HttpPost]
        [Route("loadPrint")]
        public HttpResponseMessage GetPrintVersion(PostedDataEntity loadDocumentRequest)
        {
            try
            {
                LoadDocumentEntity loadPrintDocument = GetDocumentPages(loadDocumentRequest, true, this.InputHandler, this.CacheHandler);

                // return document description
                return this.Request.CreateResponse(HttpStatusCode.OK, loadPrintDocument);
            }
            catch (Exception ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.GenerateException(ex, loadDocumentRequest.password));
            }
        }

        /// <summary>
        /// Gets page dimensions and rotation angle.
        /// </summary>
        /// <param name="page">Page object.</param>
        /// <param name="pagesInfoPath">Path to file with pages rotation angles data.</param>
        /// <returns>Page dimensions and rotation angle.</returns>
        private static PageDescriptionEntity GetPageInfo(Page page, string pagesInfoPath)
        {
            int currentAngle = GetCurrentAngle(page.Number, pagesInfoPath);

            PageDescriptionEntity pageDescriptionEntity = new PageDescriptionEntity
            {
                number = page.Number,

                // we intentionally use the 0 here because we plan to rotate only the page background using height/width
                angle = 0,
                height = currentAngle == 0 || currentAngle == 180 ? page.Height : page.Width,
                width = currentAngle == 0 || currentAngle == 180 ? page.Width : page.Height,
            };

            return pageDescriptionEntity;
        }

        /// <summary>
        /// Gets page content as a string.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="fileCachePath">File cache path.</param>
        /// <returns>Page content as a string.</returns>
        private static string GetPageContent(int pageNumber, string fileCachePath)
        {
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                string htmlFilePath = $"{fileCachePath}/p{pageNumber}.html";
                return File.ReadAllText(htmlFilePath);
            }
            else
            {
                string pngFilePath = $"{fileCachePath}/p{pageNumber}.png";

                byte[] imageBytes = null;
                using (Image image = Image.FromFile(pngFilePath))
                {
                    using (MemoryStream m = new MemoryStream())
                    {
                        image.Save(m, image.RawFormat);
                        imageBytes = m.ToArray();
                    }
                }

                return Convert.ToBase64String(imageBytes);
            }
        }

        /// <summary>
        /// Gets current rotation angle of the page.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="pagesInfoPath">Path to file with pages rotation angles data.</param>
        /// <returns>Current rotation angle of the page.</returns>
        private static int GetCurrentAngle(int pageNumber, string pagesInfoPath)
        {
            XDocument xdoc = XDocument.Load(pagesInfoPath);
            var pageData = xdoc.Descendants()?.Elements("Number")?.Where(x => int.Parse(x.Value) == pageNumber)?.Ancestors("PageData");
            var angle = pageData?.Elements("Angle").FirstOrDefault();

            if (angle != null)
            {
                return int.Parse(angle.Value);
            }

            return 0;
        }

        /// <summary>
        /// Gets document load options used in Viewer object constructor.
        /// </summary>
        /// <param name="password">Document password.</param>
        /// <returns>Load options object.</returns>
        private static Options.LoadOptions GetLoadOptions(string password)
        {
            Options.LoadOptions loadOptions = new Options.LoadOptions
            {
                Password = password,
            };

            return loadOptions;
        }

        /// <summary>
        /// Saves changed page rotation angle in cache.
        /// </summary>
        /// <param name="fileCachePath">File cache path.</param>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="newAngle">New angle value.</param>
        private static void SaveChangedAngleInCache(string fileCachePath, int pageNumber, int newAngle)
        {
            var pagesInfoPath = Path.Combine(fileCachePath, "PagesInfo.xml");

            if (File.Exists(pagesInfoPath))
            {
                XDocument xdoc = XDocument.Load(pagesInfoPath);
                var pageData = xdoc.Descendants()?.Elements("Number")?.Where(x => int.Parse(x.Value) == pageNumber)?.Ancestors("PageData");
                var angle = pageData?.Elements("Angle").FirstOrDefault();

                if (angle != null)
                {
                    angle.Value = newAngle.ToString(CultureInfo.InvariantCulture);
                }

                xdoc.Save(pagesInfoPath);
            }
        }

        /// <summary>
        /// Calculates new page rotation angle value.
        /// </summary>
        /// <param name="currentAngle">Current page rotation angle value.</param>
        /// <param name="postedAngle">Posted page rotation angle value.</param>
        /// <returns>New page rotation angle value.</returns>
        private static int GetNewAngleValue(int currentAngle, int postedAngle)
        {
            switch (currentAngle)
            {
                case 0:
                    return postedAngle == 90 ? 90 : 270;
                case 90:
                    return postedAngle == 90 ? 180 : 0;
                case 180:
                    return postedAngle == 90 ? 270 : 90;
                case 270:
                    return postedAngle == 90 ? 0 : 180;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets document pages data, dimensions and rotation angles.
        /// </summary>
        /// <param name="postedData">Posted data with document guid.</param>
        /// <param name="loadAllPages">Flag to load all pages.</param>
        /// <returns>Document pages data, dimensions and rotation angles.</returns>
        private LoadDocumentEntity GetDocumentPages(PostedDataEntity postedData, bool loadAllPages, IInputHandler inputHandler, ICacheHandler cacheHandler)
        {
            // get/set parameters
            string documentGuid = postedData.guid;
            string password = string.IsNullOrEmpty(postedData.password) ? null : postedData.password;
            string fileCachePath = cacheHandler.GetFileCachePath(documentGuid);

            IViewerCache cache = new FileViewerCache(fileCachePath);

            LoadDocumentEntity loadDocumentEntity;
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                using (HtmlViewer htmlViewer = new HtmlViewer(() => inputHandler.GetFileStream(documentGuid), inputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                {
                    loadDocumentEntity = GetLoadDocumentEntity(loadAllPages, documentGuid, fileCachePath, htmlViewer, cacheHandler);
                }
            }
            else
            {
                using (PngViewer pngViewer = new PngViewer(() => inputHandler.GetFileStream(documentGuid), inputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                {
                    loadDocumentEntity = GetLoadDocumentEntity(loadAllPages, documentGuid, fileCachePath, pngViewer, cacheHandler);
                }
            }

            return loadDocumentEntity;
        }

        private LoadDocumentEntity GetLoadDocumentEntity(bool loadAllPages, string documentGuid, string fileCachePath, ICustomViewer customViewer, ICacheHandler cacheHandler)
        {
            if (loadAllPages)
            {
                this.CacheHandler.CreateCache(customViewer);
            }

            dynamic viewInfo = customViewer.GetViewer().GetViewInfo(ViewInfoOptions.ForHtmlView());
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();

            string pagesInfoPath;
            TryCreatePagesInfoXml(fileCachePath, viewInfo, out pagesInfoPath);

            foreach (Page page in viewInfo.Pages)
            {
                PageDescriptionEntity pageData = GetPageInfo(page, pagesInfoPath);
                if (loadAllPages)
                {
                    pageData.SetData(GetPageContent(page.Number, cacheHandler.GetFileCachePath(documentGuid)));
                }

                loadDocumentEntity.SetPages(pageData);
            }

            loadDocumentEntity.SetGuid(documentGuid);

            return loadDocumentEntity;
        }

        private static void TryCreatePagesInfoXml(string fileCachePath, dynamic viewInfo, out string pagesInfoPath)
        {
            if (!Directory.Exists(fileCachePath))
            {
                Directory.CreateDirectory(fileCachePath);
            }

            pagesInfoPath = Path.Combine(fileCachePath, "PagesInfo.xml");

            if (!File.Exists(pagesInfoPath))
            {
                var xdoc = new XDocument(new XElement("Pages"));

                foreach (var page in viewInfo.Pages)
                {
                    xdoc.Element("Pages")
                        .Add(new XElement(
                            "PageData",
                            new XElement("Number", page.Number),
                            new XElement("Angle", 0)));
                }

                xdoc.Save(pagesInfoPath);
            }
        }

        private PageDescriptionEntity GetPageDescritpionEntity(ICustomViewer customViewer, string documentGuid, int pageNumber, string fileCachePath)
        {
            PageDescriptionEntity page;
            customViewer.CreateCache();

            var viewInfo = customViewer.GetViewer().GetViewInfo(ViewInfoOptions.ForHtmlView());
            page = GetPageInfo(viewInfo.Pages[pageNumber - 1], Path.Combine(fileCachePath, "PagesInfo.xml"));
            page.SetData(GetPageContent(pageNumber, this.CacheHandler.GetFileCachePath(documentGuid)));

            return page;
        }
    }
}