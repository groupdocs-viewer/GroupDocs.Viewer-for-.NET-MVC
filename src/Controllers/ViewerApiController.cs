using GroupDocs.Viewer;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Converter.Options;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Domain.Containers;
using GroupDocs.Viewer.Domain.Options;
using GroupDocs.Viewer.Exception;
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
using Viewer.Wrapper;

namespace Viewer.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ViewerApiController : ApiController
    {
        private static Config.ViewerConfig viewerConfig;
        private static ViewerHtmlHandler viewerHtmlHandler;
        private static ViewerImageHandler viewerImageHandler;

        public ViewerApiController()
        {
            viewerConfig = new Config.ViewerConfig();
            // create viewer application configuration
            ViewerConfig config = new ViewerConfig();
            config.StoragePath = viewerConfig.getApplication().getFilesDirectory();
            config.EnableCaching = true;
            List<string> fontsDirectory = new List<string>();
            fontsDirectory.Add(viewerConfig.getApplication().getFontsDirectory());
            config.FontDirectories = fontsDirectory;
            // set GroupDocs license
            License license = new License();
            license.SetLicense(viewerConfig.getApplication().getLicensePath());
            // initialize viewer instance for the HTML mode
            viewerHtmlHandler = new ViewerHtmlHandler(config);
            // initialize viewer instance for the Image mode
            viewerImageHandler = new ViewerImageHandler(config);
        }

        /// <summary>
        /// Get all files and directories from storage
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>List of files and directories</returns>
        [HttpPost]
        [Route("loadFileTree")]
        public HttpResponseMessage loadFileTree(PostedDataWrapper postedData)
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
                FileListContainer fileListContainer = viewerHtmlHandler.GetFileList(fileListOptions);

                List<FileDescriptionWrapper> fileList = new List<FileDescriptionWrapper>();
                // parse files/folders list
                foreach (FileDescription fd in fileListContainer.Files)
                {
                    FileDescriptionWrapper fileDescription = new FileDescriptionWrapper();
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
                    fileDescription.docType = fd.DocumentType;
                    // set is directory true/false
                    fileDescription.isDirectory = fd.IsDirectory;
                    // set file size
                    fileDescription.size = fd.Size;
                    // add object to array list
                    fileList.Add(fileDescription);
                }
                return Request.CreateResponse(HttpStatusCode.OK, fileList);
            }
            catch (Exception ex)
            {
                // set exception message
                ErrorMsgWrapper errorMsgWrapper = new ErrorMsgWrapper();
                errorMsgWrapper.message = ex.Message;
                errorMsgWrapper.exception = ex;
                return Request.CreateResponse(HttpStatusCode.OK, errorMsgWrapper);
            }
        }

        /// <summary>
        /// Load document description
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document info object</returns>
        [HttpPost]
        [Route("loadDocumentDescription")]
        public HttpResponseMessage LoadDocumentDescription(PostedDataWrapper postedData)
        {
            string password = "";
            string documentGuid = "";
            bool htmlMode = false;
            try
            {
                // get request body
                if (postedData != null)
                {
                    // get/set parameters
                    documentGuid = postedData.guid;
                    htmlMode = postedData.htmlMode;
                    password = postedData.password;
                    // check if documentGuid contains path or only file name
                    if (!Path.IsPathRooted(documentGuid))
                    {
                        documentGuid = viewerConfig.getApplication().getFilesDirectory() + "/" + documentGuid;
                    }
                }
                DocumentInfoContainer documentInfoContainer = new DocumentInfoContainer();
                // get document info options
                DocumentInfoOptions documentInfoOptions = new DocumentInfoOptions(documentGuid);
                // set password for protected document                
                documentInfoOptions.Password = password;
                // get document info container
                if (htmlMode)
                {
                    documentInfoContainer = viewerHtmlHandler.GetDocumentInfo(documentGuid, documentInfoOptions);
                }
                else
                {
                    documentInfoContainer = viewerImageHandler.GetDocumentInfo(documentGuid, documentInfoOptions);
                }
                // return document description
                return Request.CreateResponse(HttpStatusCode.OK, documentInfoContainer.Pages);
            }
            catch (InvalidPasswordException ex)
            {
                // Set exception message
                ErrorMsgWrapper errorMsgWrapper = new ErrorMsgWrapper();
                if (string.IsNullOrEmpty(password))
                {
                    errorMsgWrapper.message = "Password Required";
                }
                else if (!string.IsNullOrEmpty(password))
                {
                    errorMsgWrapper.message = "Incorrect password";
                }
                else
                {
                    errorMsgWrapper.message = ex.Message;
                }
                errorMsgWrapper.exception = ex;
                return Request.CreateResponse(HttpStatusCode.OK, errorMsgWrapper);
            }
            catch (Exception ex)
            {
                // set exception message
                ErrorMsgWrapper errorMsgWrapper = new ErrorMsgWrapper();
                errorMsgWrapper.message = ex.Message;
                errorMsgWrapper.exception = ex;
                return Request.CreateResponse(HttpStatusCode.OK, errorMsgWrapper);
            }
        }

        /// <summary>
        /// Get document page
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document page object</returns>
        [HttpPost]
        [Route("loadDocumentPage")]
        public HttpResponseMessage LoadDocumentPage(PostedDataWrapper postedData)
        {
            try
            {
                // get/set parameters
                string documentGuid = postedData.guid;
                int pageNumber = postedData.page;
                bool htmlMode = postedData.htmlMode;
                string password = postedData.password;
                LoadedPageWrapper loadedPage = new LoadedPageWrapper();
                string angle = "0";
                // set options
                if (htmlMode)
                {
                    HtmlOptions htmlOptions = new HtmlOptions();
                    htmlOptions.PageNumber = pageNumber;
                    htmlOptions.CountPagesToRender = 1;
                    htmlOptions.IsResourcesEmbedded = true;
                    // set password for protected document
                    if (!string.IsNullOrEmpty(password))
                    {
                        htmlOptions.Password = password;
                    }
                    // get page HTML
                    loadedPage.pageHtml = viewerHtmlHandler.GetPages(documentGuid, htmlOptions)[0].HtmlContent;
                    // get page rotation angle
                    angle = viewerHtmlHandler.GetDocumentInfo(documentGuid).Pages[pageNumber - 1].Angle.ToString();
                }
                else
                {
                    ImageOptions imageOptions = new ImageOptions();
                    imageOptions.PageNumber = pageNumber;
                    imageOptions.CountPagesToRender = 1;
                    // set password for protected document
                    if (!string.IsNullOrEmpty(password))
                    {
                        imageOptions.Password = password;
                    }

                    byte[] bytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        viewerImageHandler.GetPages(documentGuid, imageOptions)[0].Stream.CopyTo(memoryStream);
                        bytes = memoryStream.ToArray();
                    }

                    string incodedImage = Convert.ToBase64String(bytes);

                    loadedPage.pageImage = incodedImage;
                    // get page rotation angle
                    angle = viewerImageHandler.GetDocumentInfo(documentGuid).Pages[pageNumber - 1].Angle.ToString();
                }
                loadedPage.angle = angle;
                // return loaded page object
                return Request.CreateResponse(HttpStatusCode.OK, loadedPage);
            }
            catch (Exception ex)
            {
                // set exception message
                ErrorMsgWrapper errorMsgWrapper = new ErrorMsgWrapper();
                errorMsgWrapper.message = ex.Message;
                errorMsgWrapper.exception = ex;
                return Request.CreateResponse(HttpStatusCode.OK, errorMsgWrapper);
            }
        }

        /// <summary>
        /// Rotated specific page(s) of the document
        /// </summary>
        /// <param name="postedData">Document page number to rotate and rotation angle</param>
        /// <returns>Rotated document page object</returns>
        [HttpPost]
        [Route("rotateDocumentPages")]
        public HttpResponseMessage RotateDocumentPages(PostedDataWrapper postedData)
        {
            try
            {
                // get/set parameters
                string documentGuid = postedData.guid;
                int angle = postedData.angle;
                List<int> pages = postedData.pages;
                bool htmlMode = postedData.htmlMode;
                string password = postedData.password;
                // a list of the rotated pages info
                List<RotatedPageWrapper> rotatedPages = new List<RotatedPageWrapper>();
                // rotate pages
                for (int i = 0; i < pages.Count; i++)
                {
                    // prepare rotated page info object
                    RotatedPageWrapper rotatedPage = new RotatedPageWrapper();
                    int pageNumber = pages[i];
                    RotatePageOptions rotateOptions = new RotatePageOptions(pageNumber, angle);
                    // perform page rotation
                    string resultAngle = "0";
                    // set password for protected document
                    if (!string.IsNullOrEmpty(password))
                    {
                        rotateOptions.Password = password;
                    }
                    if (htmlMode)
                    {
                        viewerHtmlHandler.RotatePage(documentGuid, rotateOptions);
                        resultAngle = viewerHtmlHandler.GetDocumentInfo(documentGuid).Pages[pageNumber - 1].Angle.ToString();
                    }
                    else
                    {
                        viewerImageHandler.RotatePage(documentGuid, rotateOptions);
                        resultAngle = viewerImageHandler.GetDocumentInfo(documentGuid).Pages[pageNumber - 1].Angle.ToString();
                    }
                    // add rotated page number
                    rotatedPage.pageNumber = pageNumber;
                    // add rotated page angle
                    rotatedPage.angle = resultAngle;
                    // add rotated page object into resulting list
                    rotatedPages.Add(rotatedPage);
                }
                return Request.CreateResponse(HttpStatusCode.OK, rotatedPages);
            }
            catch (Exception ex)
            {
                // set exception message
                ErrorMsgWrapper errorMsgWrapper = new ErrorMsgWrapper();
                errorMsgWrapper.message = ex.Message;
                errorMsgWrapper.exception = ex;
                return Request.CreateResponse(HttpStatusCode.OK, errorMsgWrapper);
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
                string documentStoragePath = viewerConfig.getApplication().getFilesDirectory();
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
                                fileSavePath = GetFreeFileName(documentStoragePath, httpPostedFile.FileName);
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
                            fileSavePath = GetFreeFileName(documentStoragePath, fileName);
                        }
                        // Download the Web resource and save it into the current filesystem folder.
                        client.DownloadFile(url, fileSavePath);
                    }
                }
                UploadedDocumentWrapper uploadedDocument = new UploadedDocumentWrapper();
                uploadedDocument.guid = fileSavePath;
                return Request.CreateResponse(HttpStatusCode.OK, uploadedDocument);
            }
            catch (Exception ex)
            {
                // set exception message
                ErrorMsgWrapper errorMsgWrapper = new ErrorMsgWrapper();
                errorMsgWrapper.message = ex.Message;
                errorMsgWrapper.exception = ex;
                return Request.CreateResponse(HttpStatusCode.OK, errorMsgWrapper);
            }
        }

        /// <summary>
        /// Get free file name for uploaded file if such file already exists
        /// </summary>
        /// <param name="directory">Directory where to search files</param>
        /// <param name="fileName">Uploaded file name</param>
        /// <returns></returns>
        protected string GetFreeFileName(string directory, string fileName)
        {
            string resultFileName = "";
            try
            {
                // get all files from the directory
                string[] listOfFiles = Directory.GetFiles(directory);
                for (int i = 0; i < listOfFiles.Length; i++)
                {
                    // check if file with current name already exists
                    int number = i + 1;
                    string newFileName = Path.GetFileNameWithoutExtension(fileName) + "-Copy(" + number + ")." + Path.GetExtension(fileName);
                    resultFileName = Path.Combine(directory, newFileName);
                    if (File.Exists(resultFileName))
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return resultFileName;
        }
    }
}
