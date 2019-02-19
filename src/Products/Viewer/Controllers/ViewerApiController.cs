using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Converter.Options;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Domain.Containers;
using GroupDocs.Viewer.Domain.Options;
using GroupDocs.Viewer.Handler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Controllers
{
    /// <summary>
    /// ViewerApiController
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ViewerApiController : ApiController
    {

        private static Common.Config.GlobalConfiguration globalConfiguration;
        private static ViewerHtmlHandler viewerHtmlHandler = null;
        private static ViewerImageHandler viewerImageHandler = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewerApiController()
        {
            // Check if filesDirectory is relative or absolute path           
            globalConfiguration = new Common.Config.GlobalConfiguration();
            GroupDocs.Viewer.License license = new GroupDocs.Viewer.License();
            license.SetLicense(globalConfiguration.Application.LicensePath);
            // create viewer application configuration
            ViewerConfig config = new ViewerConfig();
            config.StoragePath = globalConfiguration.Viewer.GetFilesDirectory();
            config.EnableCaching = globalConfiguration.Viewer.GetCache();
            config.ForcePasswordValidation = true;
            List<string> fontsDirectory = new List<string>();
            if (!String.IsNullOrEmpty(globalConfiguration.Viewer.GetFontsDirectory()))
            {
                fontsDirectory.Add(globalConfiguration.Viewer.GetFontsDirectory());
            }
            config.FontDirectories = fontsDirectory;
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                // initialize Viewer instance for the HTML mode
                viewerHtmlHandler = new ViewerHtmlHandler(config);
            }
            else
            {
                // initialize Viewer instance for the Image mode
                viewerImageHandler = new ViewerImageHandler(config);
            }
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
            string relDirPath = "";
            // get posted data
            if (postedData != null)
            {
                relDirPath = postedData.path;
            }
            // get file list from storage path
            FileListOptions fileListOptions = new FileListOptions(relDirPath);
            // get temp directory name
            string tempDirectoryName = new ViewerConfig().CacheFolderName;
            try
            {
                List<FileDescriptionEntity> fileList = new List<FileDescriptionEntity>();
                if (!String.IsNullOrEmpty(globalConfiguration.Viewer.GetFilesDirectory()))
                {
                    FileListContainer fileListContainer = this.GetHandler().GetFileList(fileListOptions);
                    // parse files/folders list
                    foreach (FileDescription fd in fileListContainer.Files)
                    {
                        FileDescriptionEntity fileDescription = new FileDescriptionEntity();
                        fileDescription.guid = fd.Guid;
                        // check if current file/folder is temp directory or is hidden
                        if (tempDirectoryName.Equals(fd.Name) || new FileInfo(fileDescription.guid).Attributes.HasFlag(FileAttributes.Hidden))
                        {
                            // ignore current file and skip to next one
                            continue;
                        }
                        else
                        {
                            // set file/folder name
                            fileDescription.name = fd.Name;
                        }
                        // set file type
                        fileDescription.docType = fd.FileFormat;
                        // set is directory true/false
                        fileDescription.isDirectory = fd.IsDirectory;
                        // set file size
                        fileDescription.size = fd.Size;
                        // add object to array list
                        fileList.Add(fileDescription);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, fileList);
            }
            catch (System.Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
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
            string password = "";
            try
            {
                LoadDocumentEntity loadDocumentEntity = LoadDocument(postedData, globalConfiguration.Viewer.GetPreloadPageCount() == 0);
                // return document description
                return Request.CreateResponse(HttpStatusCode.OK, loadDocumentEntity);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex, password));
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
                // get/set parameters
                string documentGuid = postedData.guid;
                int pageNumber = postedData.page;
                password = (String.IsNullOrEmpty(postedData.password)) ? null : postedData.password;
                // get document info options
                DocumentInfoContainer documentInfoContainer = new DocumentInfoContainer();
                // get document info options
                DocumentInfoOptions documentInfoOptions = new DocumentInfoOptions();
                // set password for protected document                
                documentInfoOptions.Password = password;
                // get document info container               
                documentInfoContainer = this.GetHandler().GetDocumentInfo(documentGuid, documentInfoOptions);
                PageDescriptionEntity page = GetPageDescriptionEntities(documentInfoContainer.Pages[pageNumber - 1]);
                page.SetData(GetPageContent(documentInfoContainer.Pages[pageNumber - 1], password, documentGuid));
                // return loaded page object
                return Request.CreateResponse(HttpStatusCode.OK, page);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex, password));
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
                // get/set parameters
                string documentGuid = postedData.guid;
                int angle = postedData.angle;
                List<int> pages = postedData.pages;
                string password = postedData.password;
                DocumentInfoOptions documentInfoOptions = new DocumentInfoOptions(documentGuid);
                // set password for protected document                
                documentInfoOptions.Password = password;
                // a list of the rotated pages info
                List<RotatedPageEntity> rotatedPages = new List<RotatedPageEntity>();
                // rotate pages
                for (int i = 0; i < pages.Count; i++)
                {
                    // prepare rotated page info object
                    RotatedPageEntity rotatedPage = new RotatedPageEntity();
                    int pageNumber = pages[i];
                    RotatePageOptions rotateOptions = new RotatePageOptions(pageNumber, angle);
                    // perform page rotation
                    string resultAngle = "0";
                    // set password for protected document
                    if (!string.IsNullOrEmpty(password))
                    {
                        rotateOptions.Password = password;
                    }
                    this.GetHandler().RotatePage(documentGuid, rotateOptions);
                    resultAngle = this.GetHandler().GetDocumentInfo(documentGuid, documentInfoOptions).Pages[pageNumber - 1].Angle.ToString();
                    // add rotated page number
                    rotatedPage.SetPageNumber(pageNumber);
                    // add rotated page angle
                    rotatedPage.SetAngle(resultAngle);
                    // add rotated page object into resulting list                   
                    rotatedPages.Add(rotatedPage);
                }
                return Request.CreateResponse(HttpStatusCode.OK, rotatedPages);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
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
                UploadedDocumentEntity uploadedDocument = new UploadedDocumentEntity();
                uploadedDocument.guid = fileSavePath;
                return Request.CreateResponse(HttpStatusCode.OK, uploadedDocument);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
        }

        [HttpPost]
        [Route("loadThumbnails")]
        public LoadDocumentEntity loadThumbnails(PostedDataEntity loadDocumentRequest)
        {
            return LoadDocument(loadDocumentRequest, true);
        }

        private LoadDocumentEntity LoadDocument(PostedDataEntity postedData, bool loadAllPages)
        {
            // get/set parameters
            string documentGuid = postedData.guid;
            string password = (String.IsNullOrEmpty(postedData.password)) ? null : postedData.password;
            // check if documentGuid contains path or only file name
            if (!Path.IsPathRooted(documentGuid))
            {
                documentGuid = globalConfiguration.Viewer.GetFilesDirectory() + "/" + documentGuid;
            }
            DocumentInfoContainer documentInfoContainer;
            // get document info options
            DocumentInfoOptions documentInfoOptions = new DocumentInfoOptions();
            // set password for protected document                
            documentInfoOptions.Password = password;
            // get document info container               
            documentInfoContainer = this.GetHandler().GetDocumentInfo(documentGuid, documentInfoOptions);
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
            List<string> pagesContent = new List<string>();
            if (loadAllPages)
            {
                pagesContent = GetAllPagesContent(password, documentGuid);
            }
            foreach (PageData page in documentInfoContainer.Pages)
            {
                PageDescriptionEntity pageData = GetPageDescriptionEntities(page);
                if (pagesContent.Count > 0)
                {
                    pageData.SetData(pagesContent[page.Number - 1]);
                }
                loadDocumentEntity.SetPages(pageData);
            }
            loadDocumentEntity.SetGuid(documentGuid);
            // return document description
            return loadDocumentEntity;
        }


        private dynamic GetHandler()
        {
            if (viewerHtmlHandler != null)
            {
                return viewerHtmlHandler;
            }
            else
            {
                return viewerImageHandler;
            }
        }

        private PageDescriptionEntity GetPageDescriptionEntities(PageData page)
        {
            PageDescriptionEntity pageDescriptionEntity = new PageDescriptionEntity();
            pageDescriptionEntity.number = page.Number;
            pageDescriptionEntity.angle = page.Angle;
            pageDescriptionEntity.height = page.Height;
            pageDescriptionEntity.width = page.Width;
            return pageDescriptionEntity;
        }

        private string GetPageContent(PageData page, string password, string documentGuid)
        {
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                HtmlOptions htmlOptions = new HtmlOptions();
                SetOptions(htmlOptions, password, page.Number);
                // get page HTML              
                return this.GetHandler().GetPages(documentGuid, htmlOptions)[0].HtmlContent;

            }
            else
            {
                ImageOptions imageOptions = new ImageOptions();
                SetOptions(imageOptions, password, page.Number);
                byte[] bytes;
                using (var memoryStream = new MemoryStream())
                {
                    this.GetHandler().GetPages(documentGuid, imageOptions)[0].Stream.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }
                string encodedImage = Convert.ToBase64String(bytes);
                return encodedImage;
            }
        }

        private List<string> GetAllPagesContent(string password, string documentGuid)
        {
            List<string> allPages = new List<string>();
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                HtmlOptions htmlOptions = new HtmlOptions();
                SetOptions(htmlOptions, password, 0);
                // get page HTML              
                var pages = this.GetHandler().GetPages(documentGuid, htmlOptions);
                for (int i = 0; i < pages.Count; i++)
                {
                    allPages.Add(pages[i].HtmlContent);
                }
            }
            else
            {
                ImageOptions imageOptions = new ImageOptions();
                SetOptions(imageOptions, password, 0);
                var pages = this.GetHandler().GetPages(documentGuid, imageOptions);
                for (int i = 0; i < pages.Count; i++)
                {
                    byte[] bytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        pages[i].Stream.CopyTo(memoryStream);
                        bytes = memoryStream.ToArray();
                    }
                    string encodedImage = Convert.ToBase64String(bytes);
                    allPages.Add(encodedImage);
                }
            }
            return allPages;
        }

        private void SetOptions(HtmlOptions options, string password, int pageNumber)
        {
            Watermark watermark = null;
            if (!String.IsNullOrEmpty(globalConfiguration.Viewer.GetWatermarkText()))
            {
                // Set watermark properties
                watermark = new Watermark(globalConfiguration.Viewer.GetWatermarkText());
                watermark.Color = System.Drawing.Color.Blue;
                watermark.Position = WatermarkPosition.Diagonal;
                watermark.Width = 100;
            }
            options.EmbedResources = true;
            // set password for protected document
            if (!string.IsNullOrEmpty(password))
            {
                options.Password = password;
            }
            if (watermark != null)
            {
                options.Watermark = watermark;
            }
            if (pageNumber != 0)
            {
                options.PageNumber = pageNumber;
                options.CountPagesToRender = 1;
            }
        }

        private void SetOptions(ImageOptions options, string password, int pageNumber)
        {
            Watermark watermark = null;
            if (!String.IsNullOrEmpty(globalConfiguration.Viewer.GetWatermarkText()))
            {
                // Set watermark properties
                watermark = new Watermark(globalConfiguration.Viewer.GetWatermarkText());
                watermark.Color = System.Drawing.Color.Blue;
                watermark.Position = WatermarkPosition.Diagonal;
                watermark.Width = 100;
            }
            // set password for protected document
            if (!string.IsNullOrEmpty(password))
            {
                options.Password = password;
            }
            if (watermark != null)
            {
                options.Watermark = watermark;
            }
            if (pageNumber != 0)
            {
                options.PageNumber = pageNumber;
                options.CountPagesToRender = 1;
            }
        }
    }
}