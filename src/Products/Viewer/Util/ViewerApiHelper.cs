using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.Results;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    public class ViewerApiHelper
    {
        private readonly GlobalConfiguration globalConfiguration;

        public ViewerApiHelper(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        /// <summary>
        /// Gets page dimensions and rotation angle.
        /// </summary>
        /// <param name="page">Page object.</param>
        /// <param name="pagesInfoPath">Path to file with pages rotation angles data.</param>
        /// <returns>Page dimensions and rotation angle.</returns>
        internal static PageDescriptionEntity GetPageInfo(Page page, string pagesInfoPath)
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
        internal string GetPageContent(int pageNumber, string fileCachePath)
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
        internal static int GetCurrentAngle(int pageNumber, string pagesInfoPath)
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
        internal static Options.LoadOptions GetLoadOptions(string password)
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
        internal static void SaveChangedAngleInCache(string fileCachePath, int pageNumber, int newAngle)
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
        internal static int GetNewAngleValue(int currentAngle, int postedAngle)
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
        internal LoadDocumentEntity GetDocumentPages(PostedDataEntity postedData, bool loadAllPages, IInputHandler inputHandler, ICacheHandler cacheHandler)
        {
            // get/set parameters
            string documentGuid = postedData.guid;
            string password = string.IsNullOrEmpty(postedData.password) ? null : postedData.password;
            string fileCachePath = cacheHandler.GetFileCachePath(inputHandler.GetFileName(documentGuid));

            IViewerCache cache = new FileViewerCache(fileCachePath);

            LoadDocumentEntity loadDocumentEntity;
            if (globalConfiguration.Viewer.GetIsHtmlMode())
            {
                using (HtmlViewer htmlViewer = new HtmlViewer(() => inputHandler.GetFile(documentGuid), inputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                {
                    loadDocumentEntity = GetLoadDocumentEntity(loadAllPages, documentGuid, fileCachePath, htmlViewer, cacheHandler);
                }
            }
            else
            {
                using (PngViewer pngViewer = new PngViewer(() => inputHandler.GetFile(documentGuid), inputHandler.GetFileName(documentGuid), cache, () => GetLoadOptions(password)))
                {
                    loadDocumentEntity = GetLoadDocumentEntity(loadAllPages, documentGuid, fileCachePath, pngViewer, cacheHandler);
                }
            }

            return loadDocumentEntity;
        }

        internal LoadDocumentEntity GetLoadDocumentEntity(bool loadAllPages, string documentGuid, string fileCachePath, ICustomViewer customViewer, ICacheHandler cacheHandler)
        {
            if (loadAllPages)
            {
                cacheHandler.CreateCache(customViewer);
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
                    pageData.SetData(GetPageContent(page.Number, fileCachePath));
                }

                loadDocumentEntity.SetPages(pageData);
            }

            loadDocumentEntity.SetGuid(documentGuid);

            return loadDocumentEntity;
        }

        internal static void TryCreatePagesInfoXml(string fileCachePath, dynamic viewInfo, out string pagesInfoPath)
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

        internal PageDescriptionEntity GetPageDescritpionEntity(ICustomViewer customViewer, int pageNumber, string fileCachePath)
        {
            PageDescriptionEntity page;
            customViewer.GenerateFileCache();

            var viewInfo = customViewer.GetViewer().GetViewInfo(ViewInfoOptions.ForHtmlView());
            page = GetPageInfo(viewInfo.Pages[pageNumber - 1], Path.Combine(fileCachePath, "PagesInfo.xml"));
            page.SetData(GetPageContent(pageNumber, fileCachePath));

            return page;
        }
    }
}