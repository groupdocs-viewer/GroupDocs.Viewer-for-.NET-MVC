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

            // TODO: get temp directory name
            string tempDirectoryName = "temp";

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
                File.Delete(Path.Combine(cacheFolder, "p" + pageNumber + "_html.dat"));

                using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, settings))
                {
                    HtmlViewOptions viewOptions = HtmlViewOptions.ForEmbeddedResources();
                    var currentAngle = GetCurrentAngle(pageNumber, Path.Combine(cacheFolder, "PagesInfo.xml"));
                    int newAngle = GetNewAngleValue(currentAngle, postedData.angle);

                    if (newAngle != 0)
                    {
                        Rotation rotationAngle = GetRotationByAngle(newAngle);
                        viewOptions.RotatePage(pageNumber, rotationAngle);
                    }

                    SaveChangeAngle(settings, pageNumber, newAngle);

                    viewer.View(viewOptions);
                }

                // TODO: remove response object
                return Request.CreateResponse(HttpStatusCode.OK, new object());
            }
            catch (Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
        }

        private void SaveChangeAngle(ViewerSettings settings, int pageNumber, int newAngle)
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

            dynamic viewInfo;

            string pageFilePathFormat;
            ViewerSettings settings = GetViewerSettings(documentGuid, out pageFilePathFormat);

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, settings))
            {
                HtmlViewOptions options = HtmlViewOptions.ForEmbeddedResources(pageFilePathFormat);
                viewInfo = viewer.GetViewInfo(ViewInfoOptions.ForHtmlView());

                viewer.View(options);
            }

            return GetLoadDocumentEntity(documentGuid, settings);
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

        private LoadDocumentEntity GetLoadDocumentEntity(string documentGuid, ViewerSettings settings)
        {
            dynamic viewInfoJpg;
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
            var cacheFolder = ((FileCache)settings.Cache).CachePath;
            var pagesInfoPath = Path.Combine(cacheFolder, "PagesInfo.xml");

            // Create the list to store output pages
            List<MemoryStream> pages = new List<MemoryStream>();

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, settings))
            {
                MemoryPageStreamFactory pageStreamFactory = new MemoryPageStreamFactory(pages);

                ViewOptions viewOptions =
                    HtmlViewOptions.ForEmbeddedResources(pageStreamFactory);

                viewer.View(viewOptions);
            }

            // TODO: we need this currently to get pages sizes remove on 20.3
            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid))
            {
                viewInfoJpg = viewer.GetViewInfo(ViewInfoOptions.ForJpgView(false));
                
                if (!File.Exists(pagesInfoPath))
                {
                    CreatePagesInfoFile(pagesInfoPath, viewInfoJpg);
                }
            }

            List<string> pagesContent = new List<string>();

            foreach (var pageStream in pages)
            {
                pagesContent.Add(Encoding.UTF8.GetString(pageStream.ToArray()));
            }

            foreach (Page page in viewInfoJpg.Pages)
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

        private void CreatePagesInfoFile(string pagesInfoPath, ViewInfo viewInfoJpg)
        {
            var xdoc = new XDocument(new XElement("Pages"));

            foreach (var page in viewInfoJpg.Pages)
            {
                xdoc.Element("Pages")
                    .Add(new XElement("PageData",
                        new XElement("Number", page.Number),
                        new XElement("Angle", 0)));
            }

            xdoc.Save(pagesInfoPath);
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
            // TODO: we intentionally use the 0 here because we plan to rotate only the page background using height/width
            pageDescriptionEntity.angle = 0;
            pageDescriptionEntity.height = currentAngle == 0 || currentAngle == 180 ? page.Height : page.Width;
            pageDescriptionEntity.width = currentAngle == 0 || currentAngle == 180 ? page.Width : page.Height;
            return pageDescriptionEntity;
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
    }
}