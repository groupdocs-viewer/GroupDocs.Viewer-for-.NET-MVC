using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Xml.Linq;
using GroupDocs.Viewer.Caching;
using GroupDocs.Viewer.Exceptions;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using GroupDocs.Viewer.MVC.Products.Viewer.Config;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.Results;

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

        private static Common.Config.GlobalConfiguration globalConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerApiController"/> class.
        /// </summary>
        public ViewerApiController()
        {
            // Check if filesDirectory is relative or absolute path
            globalConfiguration = new Common.Config.GlobalConfiguration();

            List<string> fontsDirectory = new List<string>();
            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetFontsDirectory()))
            {
                fontsDirectory.Add(globalConfiguration.Viewer.GetFontsDirectory());
            }
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
                List<FileDescriptionEntity> filesList = new List<FileDescriptionEntity>();

                if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetFilesDirectory()))
                {
                    var currentPath = globalConfiguration.Viewer.GetFilesDirectory();
                    List<string> allFiles = new List<string>(Directory.GetFiles(currentPath));
                    allFiles.AddRange(Directory.GetDirectories(currentPath));

                    string cacheFolderName = globalConfiguration.Viewer.GetCacheFolderName();

                    allFiles.Sort(new FileNameComparator());
                    allFiles.Sort(new FileDateComparator());

                    foreach (string file in allFiles)
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        // check if current file/folder is hidden
                        if (!(cacheFolderName.Equals(Path.GetFileName(file)) ||
                              Path.GetFileName(file).StartsWith(".") ||
                              fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                              Path.GetFileName(file).Equals(Path.GetFileName(globalConfiguration.Viewer.GetFilesDirectory()))))
                        {
                            FileDescriptionEntity fileDescription = new FileDescriptionEntity
                            {
                                guid = Path.GetFullPath(file),
                                name = Path.GetFileName(file),

                                // set is directory true/false
                                isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory),
                            };

                            // set file size
                            if (!fileDescription.isDirectory)
                            {
                                fileDescription.size = fileInfo.Length;
                            }

                            // add object to array list
                            filesList.Add(fileDescription);
                        }
                    }
                }

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
                LoadDocumentEntity loadDocumentEntity = GetDocumentPages(postedData, globalConfiguration.Viewer.GetPreloadPageCount() == 0);

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

                string cachePath;
                ViewerSettings settings = GetViewerSettings(documentGuid, out cachePath);

                // set password for protected document
                ViewInfo viewInfo;
                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password), settings))
                {
                    viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());

                    // we must generate cache files for future using
                    GenerateViewerCache(viewer, pageNumber);
                }

                var pagesInfoPath = Path.Combine(cachePath, "PagesInfo.xml");
                PageDescriptionEntity page = GetPageInfo(viewInfo.Pages[pageNumber - 1], pagesInfoPath);
                page.SetData(GetPageContent(viewInfo.Pages[pageNumber - 1], password, documentGuid, settings));

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

                string cachePath;
                ViewerSettings settings = GetViewerSettings(documentGuid, out cachePath);

                File.Delete(Path.Combine(cachePath, $"p{pageNumber}_html.dat"));
                File.Delete(Path.Combine(cachePath, $"p{pageNumber}_png.dat"));

                PageDescriptionEntity page;

                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(postedData.password), settings))
                {
                    var currentAngle = GetCurrentAngle(pageNumber, Path.Combine(cachePath, "PagesInfo.xml"));
                    int newAngle = GetNewAngleValue(currentAngle, postedData.angle);

                    SaveChangedAngleInCache(cachePath, pageNumber, newAngle);
                    GenerateViewerCache(viewer, pageNumber, newAngle);

                    var viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());
                    page = GetPageInfo(viewInfo.Pages[pageNumber - 1], Path.Combine(cachePath, "PagesInfo.xml"));
                    page.SetData(GetPageContent(viewInfo.Pages[pageNumber - 1], postedData.password, documentGuid, settings));
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
        public LoadDocumentEntity GetPagesThumbnails(PostedDataEntity loadDocumentRequest)
        {
            return GetDocumentPages(loadDocumentRequest, true);
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
                LoadDocumentEntity loadPrintDocument = GetDocumentPages(loadDocumentRequest, true);

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
        /// Loads print pdf.
        /// </summary>
        /// <param name="loadDocumentRequest">PostedDataEntity.</param>
        /// <returns>Data of all document pages.</returns>
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
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.GenerateException(ex));
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
        /// <param name="page">Page object.</param>
        /// <param name="password">Password.</param>
        /// <param name="documentGuid">Document guid.</param>
        /// <param name="settings">Viewer settings object.</param>
        /// <returns>Page content as a string.</returns>
        private static string GetPageContent(Page page, string password, string documentGuid, ViewerSettings settings)
        {
            List<MemoryStream> pages = new List<MemoryStream>();

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password), settings))
            {
                MemoryPageStreamFactory pageStreamFactory = new MemoryPageStreamFactory(pages);

                if (globalConfiguration.Viewer.GetIsHtmlMode())
                {
                    ViewOptions viewOptions = HtmlViewOptions.ForEmbeddedResources(pageStreamFactory);
                    viewOptions.SpreadsheetOptions.TextOverflowMode = TextOverflowMode.HideText;

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
        /// Adds watermark on document if its specified in configuration file.
        /// </summary>
        /// <param name="options"></param>
        private static void SetWatermarkOptions(ViewOptions options)
        {
            Watermark watermark = null;

            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetWatermarkText()))
            {
                // Set watermark properties
                watermark = new Watermark(globalConfiguration.Viewer.GetWatermarkText())
                {
                    Color = System.Drawing.Color.Blue,
                    Position = Position.Diagonal,
                };
            }

            if (watermark != null)
            {
                options.Watermark = watermark;
            }
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
        /// Generates cache of the document when it opened for the first time.
        /// </summary>
        /// <param name="viewer">Viewer object.</param>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="newAngle">New angle value.</param>
        private static void GenerateViewerCache(GroupDocs.Viewer.Viewer viewer, int pageNumber = -1, int newAngle = 0)
        {
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                HtmlViewOptions htmlViewOptions = HtmlViewOptions.ForEmbeddedResources(_ => new MemoryStream());
                htmlViewOptions.SpreadsheetOptions.TextOverflowMode = TextOverflowMode.HideText;
                SetWatermarkOptions(htmlViewOptions);

                if (pageNumber < 0)
                {
                    viewer.View(htmlViewOptions);
                }
                else
                {
                    if (newAngle != 0)
                    {
                        Rotation rotationAngle = GetRotationByAngle(newAngle);
                        htmlViewOptions.RotatePage(pageNumber, rotationAngle);
                    }

                    viewer.View(htmlViewOptions, pageNumber);
                }
            }
            else
            {
                PngViewOptions pngViewOptions = new PngViewOptions(_ => new MemoryStream());
                SetWatermarkOptions(pngViewOptions);

                if (pageNumber < 0)
                {
                    viewer.View(pngViewOptions);
                }
                else
                {
                    if (newAngle != 0)
                    {
                        Rotation rotationAngle = GetRotationByAngle(newAngle);
                        pngViewOptions.RotatePage(pageNumber, rotationAngle);
                    }

                    viewer.View(pngViewOptions, pageNumber);
                }
            }
        }

        /// <summary>
        /// Gets viewer settings object needed for using cache files.
        /// </summary>
        /// <param name="documentGuid">Absolute path to document.</param>
        /// <param name="cachePath">Absolute path to cache files folder.</param>
        /// <returns>Viewer settings object.</returns>
        private static ViewerSettings GetViewerSettings(string documentGuid, out string cachePath)
        {
            string outputDirectory = globalConfiguration.Viewer.GetFilesDirectory();
            cachePath = Path.Combine(outputDirectory, "cache");
            cachePath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(documentGuid) + "_" + Path.GetExtension(documentGuid).Replace(".", string.Empty));

            ICache fileCache = new FileCache(cachePath);
            IKeyLockerStore keyLockerStore = new ConcurrentDictionaryKeyLockerStore(KeyLockerMap, cachePath);
            ICache threadSafeCache = new ThreadSafeCache(fileCache, keyLockerStore);

            return new ViewerSettings(threadSafeCache);
        }

        /// <summary>
        /// Saves changed page rotation angle in cache.
        /// </summary>
        /// <param name="cachePath">Cache files path.</param>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="newAngle">New angle value.</param>
        private static void SaveChangedAngleInCache(string cachePath, int pageNumber, int newAngle)
        {
            var pagesInfoPath = Path.Combine(cachePath, "PagesInfo.xml");

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
        /// Gets enumeration member by rotation angle value.
        /// </summary>
        /// <param name="newAngle">New rotation angle value.</param>
        /// <returns>Rotation enumeration member.</returns>
        private static Rotation GetRotationByAngle(int newAngle)
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

        /// <summary>
        /// Gets document pages data, dimensions and rotation angles.
        /// </summary>
        /// <param name="postedData">Posted data with document guid.</param>
        /// <param name="loadAllPages">Flag to load all pages.</param>
        /// <returns>Document pages data, dimensions and rotation angles.</returns>
        private static LoadDocumentEntity GetDocumentPages(PostedDataEntity postedData, bool loadAllPages)
        {
            // get/set parameters
            string documentGuid = postedData.guid;
            string password = string.IsNullOrEmpty(postedData.password) ? null : postedData.password;
            if (!File.Exists(documentGuid)) 
            { 
                throw new GroupDocsViewerException("File not found."); 
            }

            string cachePath;
            ViewerSettings settings = GetViewerSettings(documentGuid, out cachePath);

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, GetLoadOptions(password), settings))
            {
                if (loadAllPages)
                {
                    GenerateViewerCache(viewer);
                }

                dynamic viewInfo;
                LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();

                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }

                var pagesInfoPath = Path.Combine(cachePath, "PagesInfo.xml");
                viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());

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

                List<string> pagesContent = new List<string>();

                if (loadAllPages)
                {
                    List<MemoryStream> pages = new List<MemoryStream>();
                    MemoryPageStreamFactory pageStreamFactory = new MemoryPageStreamFactory(pages);

                    if (globalConfiguration.Viewer.GetIsHtmlMode())
                    {
                        ViewOptions viewOptions = HtmlViewOptions.ForEmbeddedResources(pageStreamFactory);
                        viewOptions.SpreadsheetOptions.TextOverflowMode = TextOverflowMode.HideText;

                        viewer.View(viewOptions);
                    }
                    else
                    {
                        PngViewOptions pngViewOptions = new PngViewOptions(pageStreamFactory);

                        viewer.View(pngViewOptions);
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
                    PageDescriptionEntity pageData = GetPageInfo(page, pagesInfoPath);

                    if (pagesContent.Count > 0)
                    {
                        pageData.SetData(pagesContent[page.Number - 1]);
                    }

                    loadDocumentEntity.SetPages(pageData);
                }

                loadDocumentEntity.SetGuid(documentGuid);

                return loadDocumentEntity;
            }
        }
    }
}