using GroupDocs.Viewer.Exceptions;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using GroupDocs.Viewer.MVC.Products.Viewer.Config;
using GroupDocs.Viewer.MVC.Products.Viewer.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
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
        
        private readonly ViewerApiHelper ViewerApiHelper;

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
            this.ViewerApiHelper = new ViewerApiHelper(globalConfiguration);
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
        public HttpResponseMessage RenderDocument(PostedDataEntity postedData)
        {
            try
            {
                LoadDocumentEntity loadDocumentEntity = this.ViewerApiHelper.GetDocumentPages(postedData, globalConfiguration.Viewer.GetPreloadPageCount() == 0, this.InputHandler, this.CacheHandler);

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
        public HttpResponseMessage RederDocumentPage(PostedDataEntity postedData)
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
                    using (HtmlViewer htmlViewer = new HtmlViewer(() => this.InputHandler.GetFile(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => ViewerApiHelper.GetLoadOptions(password)))
                    {
                        page = this.ViewerApiHelper.GetPageDescritpionEntity(htmlViewer, documentGuid, pageNumber, fileCachePath, this.CacheHandler);
                    }
                }
                else
                {
                    using (PngViewer pngViewer = new PngViewer(() => this.InputHandler.GetFile(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => ViewerApiHelper.GetLoadOptions(password)))
                    {
                        page = this.ViewerApiHelper.GetPageDescritpionEntity(pngViewer, documentGuid, pageNumber, fileCachePath, this.CacheHandler);
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
                var currentAngle = ViewerApiHelper.GetCurrentAngle(pageNumber, Path.Combine(fileCachePath, "PagesInfo.xml"));
                int newAngle = ViewerApiHelper.GetNewAngleValue(currentAngle, postedData.angle);
                ViewerApiHelper.SaveChangedAngleInCache(fileCachePath, pageNumber, newAngle);

                IViewerCache cache = new FileViewerCache(fileCachePath);
                PageDescriptionEntity page;
                if (globalConfiguration.Viewer.GetIsHtmlMode())
                {
                    using (HtmlViewer htmlViewer = new HtmlViewer(() => this.InputHandler.GetFile(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => ViewerApiHelper.GetLoadOptions(password), pageNumber, newAngle))
                    {
                        page = ViewerApiHelper.GetPageDescritpionEntity(htmlViewer, documentGuid, pageNumber, fileCachePath, this.CacheHandler);
                    }
                }
                else
                {
                    using (PngViewer pngViewer = new PngViewer(() => this.InputHandler.GetFile(documentGuid), this.InputHandler.GetFileName(documentGuid), cache, () => ViewerApiHelper.GetLoadOptions(password)))
                    {
                        page = ViewerApiHelper.GetPageDescritpionEntity(pngViewer, documentGuid, pageNumber, fileCachePath, this.CacheHandler);
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
            if (!string.IsNullOrEmpty(path))
            {
                var fileStream = this.InputHandler.GetFile(path);
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(fileStream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = this.InputHandler.GetFileName(path);
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
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
                string url = HttpContext.Current.Request.Form["url"];

                // get documents storage path
                string documentStoragePath = globalConfiguration.Viewer.GetFilesDirectory();
                bool rewrite = bool.Parse(HttpContext.Current.Request.Form["rewrite"]);
                string fileSavePath = string.Empty;
                if (string.IsNullOrEmpty(url))
                {
                    if (HttpContext.Current.Request.Files.AllKeys != null)
                    {
                        // Get the uploaded document from the Files collection.
                        var httpPostedFile = HttpContext.Current.Request.Files["file"];
                        if (httpPostedFile != null)
                        {
                            fileSavePath = this.InputHandler.StoreFile(httpPostedFile.InputStream, httpPostedFile.FileName, rewrite);
                        }
                    }
                }
                else
                {
                    using (WebClient client = new WebClient())
                    {
                        // Get file name from the URL.
                        Uri uri = new Uri(url);
                        string fileName = Path.GetFileName(uri.LocalPath);
                        var byteArray = client.DownloadData(url);
                        Stream fileStream = new MemoryStream(byteArray);

                        fileSavePath = this.InputHandler.StoreFile(fileStream, fileName, rewrite);
                    }
                }

                UploadedDocumentEntity uploadedDocument = new UploadedDocumentEntity
                {
                    guid = fileSavePath,
                };

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
        public LoadDocumentEntity RenderDocumentThumbnails(PostedDataEntity loadDocumentRequest)
        {
            return ViewerApiHelper.GetDocumentPages(loadDocumentRequest, true, this.InputHandler, this.CacheHandler);
        }

        /// <summary>
        /// Loads print version.
        /// </summary>
        /// <param name="loadDocumentRequest">PostedDataEntity.</param>
        /// <returns>Data of all document pages.</returns>
        [HttpPost]
        [Route("loadPrint")]
        public HttpResponseMessage PrintDocument(PostedDataEntity loadDocumentRequest)
        {
            try
            {
                LoadDocumentEntity loadPrintDocument = ViewerApiHelper.GetDocumentPages(loadDocumentRequest, true, this.InputHandler, this.CacheHandler);

                // return document description
                return this.Request.CreateResponse(HttpStatusCode.OK, loadPrintDocument);
            }
            catch (Exception ex)
            {
                // set exception message
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.GenerateException(ex, loadDocumentRequest.password));
            }
        }
    }
}