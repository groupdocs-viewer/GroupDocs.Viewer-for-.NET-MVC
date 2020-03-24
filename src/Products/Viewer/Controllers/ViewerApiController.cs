using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using GroupDocs.Viewer.MVC.Products.Viewer.Config;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using GroupDocs.Viewer.Results;
using System.Text;
using System.Globalization;
using GroupDocs.Viewer;

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
            if (!String.IsNullOrEmpty(globalConfiguration.Viewer.GetFontsDirectory()))
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
            string relDirPath = "";
            // get posted data
            if (postedData != null)
            {
                relDirPath = postedData.path;
            }

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

            // TODO: get temp directory name
            string tempDirectoryName = "temp";

            try
            {
                allFiles.Sort(new FileNameComparator());
                allFiles.Sort(new FileDateComparator());

                foreach (string file in allFiles)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    // check if current file/folder is hidden
                    if (tempDirectoryName.Equals(Path.GetFileName(file)) ||
                        fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                        Path.GetFileName(file).Equals(Path.GetFileName(globalConfiguration.Viewer.GetFilesDirectory())))
                    {
                        // ignore current file and skip to next one
                        continue;
                    }
                    else
                    {
                        FileDescriptionEntity fileDescription = new FileDescriptionEntity();
                        fileDescription.guid = Path.GetFullPath(file);
                        fileDescription.name = Path.GetFileName(file);
                        // set is directory true/false
                        fileDescription.isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory);
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
            catch (Exception ex)
            {
                throw ex;
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
                // TODO: return InternalServerError for common Exception and Forbidden for PasswordProtectedException
                return Request.CreateResponse(HttpStatusCode.Forbidden, new Resources().GenerateException(ex, password));
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
                ViewInfo viewInfo = null;
                // set password for protected document                
                var loadOptions = GetLoadOptions(documentGuid, password);

                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, loadOptions))
                {
                    viewInfo = viewer.GetViewInfo(
                        ViewInfoOptions.ForJpgView(false));
                }

                PageDescriptionEntity page = GetPageDescriptionEntities(viewInfo.Pages[pageNumber - 1]);
                page.SetData(GetPageContent(viewInfo.Pages[pageNumber - 1], password, documentGuid));
                // return loaded page object
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
        //[HttpPost]
        //[Route("rotateDocumentPages")]
        //public HttpResponseMessage RotateDocumentPages(PostedDataEntity postedData)
        //{
        //    try
        //    {
        //        // get/set parameters
        //        string documentGuid = postedData.guid;
        //        int angle = postedData.angle;
        //        List<int> pages = postedData.pages;
        //        string password = postedData.password;
        //        //DocumentInfoOptions documentInfoOptions = new DocumentInfoOptions(documentGuid);
        //        // set password for protected document                
        //        //documentInfoOptions.Password = password;
        //        // TODO: how properly set FileType
        //        GroupDocs.Viewer.Common.Func<LoadOptions> getLoadOptions =
        //                () => new LoadOptions(FileType.DOCX, password);
        //        // a list of the rotated pages info
        //        List<RotatedPageEntity> rotatedPages = new List<RotatedPageEntity>();
        //        // rotate pages
        //        for (int i = 0; i < pages.Count; i++)
        //        {
        //            // prepare rotated page info object
        //            RotatedPageEntity rotatedPage = new RotatedPageEntity();
        //            int pageNumber = pages[i];
        //            //RotatePageOptions rotateOptions = new RotatePageOptions(pageNumber, angle);
        //            // perform page rotation
        //            string resultAngle = "0";
        //            // set password for protected document
        //            if (!string.IsNullOrEmpty(password))
        //            {
        //                //rotateOptions.Password = password;
        //            }
        //            // TODO:
        //            //this.GetHandler().RotatePage(documentGuid, rotateOptions);
        //            //resultAngle = this.GetHandler().GetDocumentInfo(documentGuid, documentInfoOptions).Pages[pageNumber - 1].Angle.ToString();
        //            // add rotated page number
        //            rotatedPage.SetPageNumber(pageNumber);
        //            // add rotated page angle
        //            rotatedPage.SetAngle(resultAngle);
        //            // add rotated page object into resulting list                   
        //            rotatedPages.Add(rotatedPage);
        //        }
        //        return Request.CreateResponse(HttpStatusCode.OK, rotatedPages);
        //    }
        //    catch (Exception ex)
        //    {
        //        // set exception message
        //        return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
        //    }
        //}

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
            catch (Exception ex)
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
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex, loadDocumentRequest.password));
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
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
        }

        private LoadDocumentEntity LoadDocument(PostedDataEntity postedData, bool loadAllPages)
        {
            // get/set parameters
            string documentGuid = postedData.guid;
            string password = (String.IsNullOrEmpty(postedData.password)) ? null : postedData.password;
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
            
            // check if documentGuid contains path or only file name
            if (!Path.IsPathRooted(documentGuid))
            {
                documentGuid = globalConfiguration.Viewer.GetFilesDirectory() + "/" + documentGuid;
            }

            dynamic viewInfo;
            dynamic viewInfoJpg = null;

            // set password for protected document                
            var loadOptions = GetLoadOptions(documentGuid, password);

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, loadOptions))
            {
                viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());
            }

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, loadOptions))
            {
                // TODO: check that this is needed
                //HtmlViewOptions viewOptions =
                //    HtmlViewOptions.ForEmbeddedResources();

                viewInfoJpg = viewer.GetViewInfo(ViewInfoOptions.ForJpgView(false));
            }

            List<string> pagesContent = new List<string>();

            if (loadAllPages)
            {
                pagesContent = GetAllPagesContent(password, documentGuid, viewInfo.Pages);
            }

            foreach (Page page in viewInfoJpg.Pages)
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

        private PageDescriptionEntity GetPageDescriptionEntities(Page page)
        {
            PageDescriptionEntity pageDescriptionEntity = new PageDescriptionEntity();
            pageDescriptionEntity.number = page.Number;
            // TODO: because page.Angle does not exist
            pageDescriptionEntity.angle = 0;
            pageDescriptionEntity.height = page.Height;
            pageDescriptionEntity.width = page.Width;
            return pageDescriptionEntity;
        }

        private string RenderPageToString(int pageNumberToRender, string documentGuid, string password)
        {
            // TODO: consider adding usage of the RedisCache
            using (MemoryStream pageStream = RenderPageToMemoryStream(pageNumberToRender, password, documentGuid))
            {
                string html = Encoding.UTF8.GetString(pageStream.ToArray());

                return html;
            }
        }

        private MemoryStream RenderPageToMemoryStream(int pageNumberToRender, string documentGuid, string password)
        {
            MemoryStream result = new MemoryStream();

            var loadOptions = GetLoadOptions(documentGuid, password);

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, loadOptions))
            {
                HtmlViewOptions viewOptions =
                    HtmlViewOptions.ForEmbeddedResources(
                        pageNumber => result,
                        (pageNumber, pageStream) =>
                        {
                            // Do not close stream as we're about to read from it
                        });

                SetWatermarkOptions(viewOptions);

                viewer.View(viewOptions, pageNumberToRender);
            }

            return result;
        }

        private string GetPageContent(Page page, string password, string documentGuid)
        {
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                // get page HTML
                return RenderPageToString(page.Number, password, documentGuid);
            }

            return page.Lines.ToString();
        }

        private List<string> GetAllPagesContent(string password, string documentGuid, IList<Page> pages)
        {
            List<string> allPages = new List<string>();
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                //get page HTML
                for (int i = 0; i < pages.Count; i++)
                {               
                    allPages.Add(RenderPageToString(pages[i].Number, password, documentGuid));
                }
            }

            return allPages;
        }

        private void SetWatermarkOptions(HtmlViewOptions options)
        {
            Watermark watermark = null;
            if (!String.IsNullOrEmpty(globalConfiguration.Viewer.GetWatermarkText()))
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

        private LoadOptions GetLoadOptions(string guid, string password)
        {
            var loadOptions = new LoadOptions(GetSaveFormat(guid)) 
            { 
                Password = password
            };

            return loadOptions;
        }

        private FileType GetSaveFormat(string saveFilePath)
        {
            string extension = Path.GetExtension(saveFilePath).Replace(".", "");
            extension = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(extension);
            dynamic format = null;
            switch (extension)
            {
                case "Doc":
                    format = FileType.DOC;
                    break;
                case "Rtf":
                    format = FileType.RTF;
                    break;
                case "Xls":
                    format = FileType.XLS;
                    break;
                case "Xlsx":
                    format = FileType.XLSX;
                    break;
                case "Pdf":
                    format = FileType.PDF;
                    break;
                default:
                    format = FileType.DOCX;
                    break;

            }
            return format;
        }
    }
}