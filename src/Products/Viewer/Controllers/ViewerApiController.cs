using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using GroupDocs.Viewer.MVC.Products.Viewer.Config;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using GroupDocs.Viewer.Results;
using System.Text;
using GroupDocs.Viewer.Exceptions;
using GroupDocs.Viewer.Caching;
using GroupDocs.Viewer.Interfaces;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Net.Http.Headers;
using System.Threading;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Controllers
{
    /// <summary>
    /// ViewerApiController
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ViewerApiController : ApiController
    {
        private static Common.Config.GlobalConfiguration globalConfiguration;

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewerApiController()
        {
            // Check if filesDirectory is relative or absolute path           
            globalConfiguration = new Common.Config.GlobalConfiguration();

            License license = new License();
            license.SetLicense(globalConfiguration.Application.LicensePath);

            List<string> fontsDirectory = new List<string>();
            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetFontsDirectory()))
            {
                fontsDirectory.Add(globalConfiguration.Viewer.GetFontsDirectory());
            }
        }

        /// <summary>
        /// Load Viewr configuration
        /// </summary>       
        /// <returns>Viewer configuration</returns>
        [HttpGet]
        [Route("loadConfig")]
        public ViewerConfiguration LoadConfig()
        {            
            return globalConfiguration.Viewer;
        }

        /// <summary>
        /// Get all files and directories from storage
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>List of files and directories</returns>
        [HttpPost]
        [Route("loadFileTree")]
        public HttpResponseMessage loadFileTree(PostedDataEntity postedData)
        {
            try
            {
                List<FileDescriptionEntity> filesList = new List<FileDescriptionEntity>();
                if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetFilesDirectory()))
                {
                    filesList = this.LoadFiles();
                }
                return Request.CreateResponse(HttpStatusCode.OK, filesList);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
        }

        /// <summary>
        /// Load documents
        /// </summary>
        /// <returns>List[FileDescriptionEntity]</returns>
        public List<FileDescriptionEntity> LoadFiles()
        {
            var currentPath = globalConfiguration.Viewer.GetFilesDirectory();
            List<string> allFiles = new List<string>(Directory.GetFiles(currentPath));
            allFiles.AddRange(Directory.GetDirectories(currentPath));
            List<FileDescriptionEntity> fileList = new List<FileDescriptionEntity>();

            string cacheFolderName = globalConfiguration.Viewer.GetCacheFolderName();

            allFiles.Sort(new FileNameComparator());
            allFiles.Sort(new FileDateComparator());

            foreach (string file in allFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                // check if current file/folder is hidden
                if (cacheFolderName.Equals(Path.GetFileName(file)) ||
                    fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                    Path.GetFileName(file).Equals(Path.GetFileName(globalConfiguration.Viewer.GetFilesDirectory())))
                {
                    // ignore current file and skip to next one
                    continue;
                }
                else
                {
                    FileDescriptionEntity fileDescription = new FileDescriptionEntity
                    {
                        guid = Path.GetFullPath(file),
                        name = Path.GetFileName(file),
                        // set is directory true/false
                        isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory)
                    };
                    // set file size
                    if (!fileDescription.isDirectory)
                    {
                        fileDescription.size = fileInfo.Length;
                    }
                    // add object to array list
                    fileList.Add(fileDescription);
                }
            }

            return fileList;
        }

        /// <summary>
        /// Load document description
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document info object</returns>
        [HttpPost]
        [Route("loadDocumentDescription")]
        public HttpResponseMessage LoadDocumentDescription(PostedDataEntity postedData)
        {
            try
            {
                LoadDocumentEntity loadDocumentEntity = LoadDocument(postedData, globalConfiguration.Viewer.GetPreloadPageCount() == 0);
                // return document description
                return Request.CreateResponse(HttpStatusCode.OK, loadDocumentEntity);
            }
            catch (PasswordRequiredException ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.Forbidden, new Resources().GenerateException(ex, postedData.password));
            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new Resources().GenerateException(ex, postedData.password));
            }
        }

        /// <summary>
        /// Get document page
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document page object</returns>
        [HttpPost]
        [Route("loadDocumentPage")]
        public HttpResponseMessage LoadDocumentPage(PostedDataEntity postedData)
        {
            string password = "";
            try
            {
                string documentGuid = postedData.guid;
                int pageNumber = postedData.page;
                password = string.IsNullOrEmpty(postedData.password) ? null : postedData.password;
                ViewInfo viewInfo = null;

                // set password for protected document
                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password)))
                {
                    viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());
                }

                string pageFilePathFormat;
                ViewerSettings settings = GetViewerSettings(documentGuid, out pageFilePathFormat);
                var cacheFolder = ((FileCache)settings.Cache).CachePath;

                EventWaitHandle waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, "SHARED_BY_ALL_PROCESSES");
                waitHandle.WaitOne();
                GenerateViewerCache(documentGuid, password, pageFilePathFormat, settings, pageNumber);
                waitHandle.Set();

                var pagesInfoPath = Path.Combine(cacheFolder, "PagesInfo.xml");
                PageDescriptionEntity page = GetPageDescriptionEntities(viewInfo.Pages[pageNumber - 1], pagesInfoPath);
                page.SetData(GetPageContent(viewInfo.Pages[pageNumber - 1], password, documentGuid, settings));

                return Request.CreateResponse(HttpStatusCode.OK, page);
            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.Forbidden, new Resources().GenerateException(ex, password));
            }
        }

        /// <summary>
        /// Rotated specific page(s) of the document
        /// </summary>
        /// <param name="postedData">Document page number to rotate and rotation angle</param>
        /// <returns>Rotated document page object</returns>
        [HttpPost]
        [Route("rotateDocumentPages")]
        public HttpResponseMessage RotateDocumentPages(PostedDataEntity postedData)
        {
            try
            {
                var documentGuid = postedData.guid;
                var pageNumber = postedData.pages[0];

                string pageFilePathFormat;
                ViewerSettings settings = GetViewerSettings(documentGuid, out pageFilePathFormat);

                var cacheFolder = ((FileCache)settings.Cache).CachePath;
                File.Delete(Path.Combine(cacheFolder, $"p{pageNumber}_html.dat"));
                File.Delete(Path.Combine(cacheFolder, $"p{pageNumber}_png.dat"));

                var page = new PageDescriptionEntity();

                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(postedData.password), settings))
                {
                    ViewOptions viewOptions;

                    if (globalConfiguration.Viewer.GetIsHtmlMode())
                    {
                        viewOptions = HtmlViewOptions.ForEmbeddedResources();
                    }
                    else
                    {
                        viewOptions = new PngViewOptions();
                    }

                    var currentAngle = GetCurrentAngle(pageNumber, Path.Combine(cacheFolder, "PagesInfo.xml"));
                    int newAngle = GetNewAngleValue(currentAngle, postedData.angle);

                    if (newAngle != 0)
                    {
                        Rotation rotationAngle = GetRotationByAngle(newAngle);
                        viewOptions.RotatePage(pageNumber, rotationAngle);
                    }

                    SaveChangedAngleInCache(settings, pageNumber, newAngle);

                    viewer.View(viewOptions);

                    var viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());
                    page = GetPageDescriptionEntities(viewInfo.Pages[pageNumber - 1], Path.Combine(cacheFolder, "PagesInfo.xml"));
                    page.SetData(GetPageContent(viewInfo.Pages[pageNumber - 1], postedData.password, documentGuid, settings));
                }

                return Request.CreateResponse(HttpStatusCode.OK, page);
            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new Resources().GenerateException(ex));
            }
        }

        /// <summary>
        /// Download curerntly viewed document
        /// </summary>
        /// <param name="path">Path of the document to download</param>
        /// <returns>Document stream as attachement</returns>
        [HttpGet]
        [Route("downloadDocument")]
        public HttpResponseMessage DownloadDocument(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    var fileStream = new FileStream(path, FileMode.Open);
                    response.Content = new StreamContent(fileStream);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                    response.Content.Headers.ContentDisposition.FileName = Path.GetFileName(path);
                    return response;
                }
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Upload document
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Uploaded document object</returns>
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
                string fileSavePath = "";
                if (string.IsNullOrEmpty(url))
                {
                    if (HttpContext.Current.Request.Files.AllKeys != null)
                    {
                        // Get the uploaded document from the Files collection
                        var httpPostedFile = HttpContext.Current.Request.Files["file"];
                        if (httpPostedFile != null)
                        {
                            if (rewrite)
                            {
                                // Get the complete file path
                                fileSavePath = Path.Combine(documentStoragePath, httpPostedFile.FileName);
                            }
                            else
                            {
                                fileSavePath = Resources.GetFreeFileName(documentStoragePath, httpPostedFile.FileName);
                            }

                            // Save the uploaded file to "UploadedFiles" folder
                            httpPostedFile.SaveAs(fileSavePath);
                        }
                    }
                }
                else
                {
                    using (WebClient client = new WebClient())
                    {
                        // get file name from the URL
                        Uri uri = new Uri(url);
                        string fileName = Path.GetFileName(uri.LocalPath);
                        if (rewrite)
                        {
                            // Get the complete file path
                            fileSavePath = Path.Combine(documentStoragePath, fileName);
                        }
                        else
                        {
                            fileSavePath = Resources.GetFreeFileName(documentStoragePath, fileName);
                        }
                        // Download the Web resource and save it into the current filesystem folder.
                        client.DownloadFile(url, fileSavePath);
                    }
                }

                UploadedDocumentEntity uploadedDocument = new UploadedDocumentEntity
                {
                    guid = fileSavePath
                };

                return Request.CreateResponse(HttpStatusCode.OK, uploadedDocument);
            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new Resources().GenerateException(ex));
            }
        }

        [HttpPost]
        [Route("loadThumbnails")]
        public LoadDocumentEntity loadThumbnails(PostedDataEntity loadDocumentRequest)
        {
            return LoadDocument(loadDocumentRequest, true);
        }

        [HttpPost]
        [Route("loadPrint")]
        public HttpResponseMessage loadPrint(PostedDataEntity loadDocumentRequest)
        {
            try
            {
                LoadDocumentEntity loadPrintDocument = LoadDocument(loadDocumentRequest, true);
                // return document description
                return Request.CreateResponse(HttpStatusCode.OK, loadPrintDocument);

            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new Resources().GenerateException(ex, loadDocumentRequest.password));
            }
        }

        [HttpPost]
        [Route("printPdf")]
        public HttpResponseMessage PrintPdf(PostedDataEntity loadDocumentRequest)
        {
            // get document path
            string documentGuid = loadDocumentRequest.guid;
            string fileName = Path.GetFileName(documentGuid);
            try
            {
                var fileStream = new FileStream(documentGuid, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(fileStream);
                // add file into the response
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = Path.GetFileName(fileName);
                return response;
            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new Resources().GenerateException(ex));
            }
        }

        private void SaveChangedAngleInCache(ViewerSettings settings, int pageNumber, int newAngle)
        {
            var cacheFolder = ((FileCache)settings.Cache).CachePath;
            var pagesInfoPath = Path.Combine(cacheFolder, "PagesInfo.xml");

            if (File.Exists(pagesInfoPath)) 
            {
                XDocument xdoc = XDocument.Load(pagesInfoPath);
                var pageData = xdoc.Descendants()?.Elements("Number")?.Where(x => int.Parse(x.Value) == pageNumber)?.Ancestors("PageData");
                var angle = pageData.Elements("Angle").FirstOrDefault();

                if (angle != null)
                {
                    angle.Value = newAngle.ToString(CultureInfo.InvariantCulture);
                }

                xdoc.Save(pagesInfoPath);
            }
        }

        private int GetNewAngleValue(int currentAngle, int postedAngle)
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

        private Rotation GetRotationByAngle(int newAngle)
        {
            switch (newAngle)
            {
                case 90:
                    return Rotation.On90Degree;
                case 180:
                    return Rotation.On180Degree;
                case 270:
                    return Rotation.On270Degree;
                default:
                    return Rotation.On90Degree;
            }
        }

        private LoadDocumentEntity LoadDocument(PostedDataEntity postedData, bool loadAllPages)
        {
            // get/set parameters
            string documentGuid = postedData.guid;
            string password = (string.IsNullOrEmpty(postedData.password)) ? null : postedData.password;

            string pageFilePathFormat;
            ViewerSettings settings = GetViewerSettings(documentGuid, out pageFilePathFormat);

            if (loadAllPages)
            {
                GenerateViewerCache(documentGuid, password, pageFilePathFormat, settings);
            }

            return GetLoadDocumentEntity(documentGuid, password, settings, loadAllPages);
        }

        private void GenerateViewerCache(string documentGuid, string password, string pageFilePathFormat, ViewerSettings settings, int pageNumber = -1)
        {
            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password), settings))
            {
                if (globalConfiguration.Viewer.GetIsHtmlMode())
                {
                    HtmlViewOptions htmlViewOptions = HtmlViewOptions.ForEmbeddedResources(pageFilePathFormat);                  
                    SetWatermarkOptions(htmlViewOptions);

                    if (pageNumber < 0)
                    {
                        viewer.View(htmlViewOptions);
                    }
                    else
                    {
                        viewer.View(htmlViewOptions, pageNumber);
                    }
                }
                else
                {
                    PngViewOptions pngViewOptions = new PngViewOptions();
                    SetWatermarkOptions(pngViewOptions);

                    if (pageNumber < 0)
                    {
                        viewer.View(pngViewOptions);
                    }
                    else
                    {
                        viewer.View(pngViewOptions, pageNumber);
                    }
                }
            }
        }

        private ViewerSettings GetViewerSettings(string documentGuid, out string pageFilePathFormat)
        {
            string outputDirectory = globalConfiguration.Viewer.GetFilesDirectory();
            string cachePath = Path.Combine(outputDirectory, "cache");
            cachePath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(documentGuid) + "_" + Path.GetExtension(documentGuid).Replace(".", string.Empty));
            pageFilePathFormat = Path.Combine(cachePath, "page_{0}.html");
            FileCache cache = new FileCache(cachePath);
            var settings = new ViewerSettings(cache);
            
            return settings;
        }

        private LoadDocumentEntity GetLoadDocumentEntity(string documentGuid, string password, ViewerSettings settings, bool loadAllPages)
        {
            dynamic viewInfo;
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
            var fileCacheFolder = ((FileCache)settings.Cache).CachePath;
            if (!Directory.Exists(fileCacheFolder))
            {
                Directory.CreateDirectory(fileCacheFolder);
            }
            var pagesInfoPath = Path.Combine(fileCacheFolder, "PagesInfo.xml");

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password)))
            {
                viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());

                if (!File.Exists(pagesInfoPath))
                {
                    CreatePagesInfoFile(pagesInfoPath, viewInfo);
                }
            }

            List<string> pagesContent = new List<string>();
            if (loadAllPages)
            {
                List<MemoryStream> pages = new List<MemoryStream>();

                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password), settings))
                {
                    MemoryPageStreamFactory pageStreamFactory = new MemoryPageStreamFactory(pages);

                    if (globalConfiguration.Viewer.GetIsHtmlMode())
                    {
                        ViewOptions viewOptions = HtmlViewOptions.ForEmbeddedResources(pageStreamFactory);

                        viewer.View(viewOptions);
                    }
                    else
                    {
                        PngViewOptions pngViewOptions = new PngViewOptions(pageStreamFactory);

                        viewer.View(pngViewOptions);
                    }
                }

                foreach (var pageStream in pages)
                {
                    if (globalConfiguration.Viewer.GetIsHtmlMode())
                    {
                        pagesContent.Add(Encoding.UTF8.GetString(pageStream.ToArray()));
                    }
                    else
                    {
                        pagesContent.Add(Convert.ToBase64String(pageStream.ToArray()));
                    }
                }
            }

            foreach (Page page in viewInfo.Pages)
            {
                PageDescriptionEntity pageData = GetPageDescriptionEntities(page, pagesInfoPath);
                if (pagesContent.Count > 0)
                {
                    pageData.SetData(pagesContent[page.Number - 1]);
                }
                loadDocumentEntity.SetPages(pageData);
            }

            loadDocumentEntity.SetGuid(documentGuid);

            return loadDocumentEntity;
        }

        internal class MemoryPageStreamFactory : IPageStreamFactory
        {
            private readonly List<MemoryStream> _pages;

            public MemoryPageStreamFactory(List<MemoryStream> pages)
            {
                _pages = pages;
            }

            public Stream CreatePageStream(int pageNumber)
            {
                MemoryStream pageStream = new MemoryStream();

                _pages.Add(pageStream);

                return pageStream;
            }

            public void ReleasePageStream(int pageNumber, Stream pageStream)
            {
                //Do not release page stream as we'll need to keep the stream open
            }
        }

        private PageDescriptionEntity GetPageDescriptionEntities(Page page, string pagesInfoPath)
        {
            int currentAngle = GetCurrentAngle(page.Number, pagesInfoPath);

            PageDescriptionEntity pageDescriptionEntity = new PageDescriptionEntity();
            pageDescriptionEntity.number = page.Number;
            // we intentionally use the 0 here because we plan to rotate only the page background using height/width
            pageDescriptionEntity.angle = 0;
            pageDescriptionEntity.height = currentAngle == 0 || currentAngle == 180 ? page.Height : page.Width;
            pageDescriptionEntity.width = currentAngle == 0 || currentAngle == 180 ? page.Width : page.Height;
            return pageDescriptionEntity;
        }

        private string GetPageContent(Page page, string password, string documentGuid, ViewerSettings settings)
        {
            List<MemoryStream> pages = new List<MemoryStream>();

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password), settings))
            {
                MemoryPageStreamFactory pageStreamFactory = new MemoryPageStreamFactory(pages);
                if (globalConfiguration.Viewer.GetIsHtmlMode())
                {
                    ViewOptions viewOptions = HtmlViewOptions.ForEmbeddedResources(pageStreamFactory);

                    viewer.View(viewOptions, page.Number);

                    return Encoding.UTF8.GetString(pages[0].ToArray());
                }
                else
                {
                    PngViewOptions pngViewOptions = new PngViewOptions(pageStreamFactory);

                    viewer.View(pngViewOptions, page.Number);

                    return Convert.ToBase64String(pages[0].ToArray());
                }
            }
        }

        private void CreatePagesInfoFile(string pagesInfoPath, ViewInfo viewInfo)
        {
            var xdoc = new XDocument(new XElement("Pages"));

            foreach (var page in viewInfo.Pages)
            {
                xdoc.Element("Pages")
                    .Add(new XElement("PageData",
                        new XElement("Number", page.Number),
                        new XElement("Angle", 0)));
            }

            xdoc.Save(pagesInfoPath);
        }

        private int GetCurrentAngle(int pageNumber, string pagesInfoPath)
        {
            XDocument xdoc = XDocument.Load(pagesInfoPath);
            var pageData = xdoc.Descendants()?.Elements("Number")?.Where(x => int.Parse(x.Value) == pageNumber)?.Ancestors("PageData");
            var angle = pageData.Elements("Angle").FirstOrDefault();

            if (angle != null)
            {
                return int.Parse(angle.Value);
            }

            return 0;
        }

        private void SetWatermarkOptions(ViewOptions options)
        {
            Watermark watermark = null;
            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetWatermarkText()))
            {
                // Set watermark properties
                watermark = new Watermark(globalConfiguration.Viewer.GetWatermarkText());
                watermark.Color = System.Drawing.Color.Blue;
                watermark.Position = Position.Diagonal;
            }

            if (watermark != null)
            {
                options.Watermark = watermark;
            }
        }

        private Options.LoadOptions GetLoadOptions(string password)
        {
            Options.LoadOptions loadOptions = new Options.LoadOptions()
            {
                Password = password
            };

            return loadOptions;
        }
    }
}