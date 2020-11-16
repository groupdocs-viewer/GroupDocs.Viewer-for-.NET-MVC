using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.Results;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    public class ViewerUtil
    {
        private static readonly string ViewerCacheFolderName = "viewer";
        private static readonly Common.Config.GlobalConfiguration globalConfiguration = new Common.Config.GlobalConfiguration();
        private static readonly string cachePath = Path.Combine(globalConfiguration.Viewer.GetFilesDirectory(), globalConfiguration.Viewer.GetCacheFolderName());

        public static LoadDocumentEntity GetCacheFor(string filePath)
        {
            var fileFolderName = Path.GetFileName(filePath).Replace(".", "_");
            string fileCachePath = Path.Combine(ViewerUtil.cachePath, fileFolderName);
            var cachePath = Path.Combine(fileCachePath, "cache.json");

            LoadDocumentEntity result = new LoadDocumentEntity();
            using (StreamReader sr = new StreamReader(cachePath))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    result = serializer.Deserialize<LoadDocumentEntity>(reader);
                }
            }

            return result;
        }

        public static void GenerateCacheFor(string filePath, string password)
        {
            var fileFolderName = Path.GetFileName(filePath).Replace(".", "_");
            string fileCachePath = Path.Combine(cachePath, fileFolderName);

            //if (Directory.Exists(fileCachePath))
            //{
            //    Directory.Delete(fileCachePath);
            //}

            LoadOptions loadOptions = new LoadOptions { Password = password };
            LoadDocumentEntity loadDocumentEntity = GetLoadDocumentEntity(loadOptions, filePath, fileCachePath);
            using (TextWriter textWriter = File.CreateText(Path.Combine(fileCachePath, "cache.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Serialize(textWriter, loadDocumentEntity);
            }
        }

        public static LoadDocumentEntity GetLoadDocumentEntity(LoadOptions loadOptions, string documentGuid, string cachePath)
        {
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
            var urlPrefix = "/viewer/resources/" + Path.GetFileName(documentGuid).Replace(".", "_");
            ExternalResourcesStreamFactory streamFactory = new ExternalResourcesStreamFactory(cachePath, urlPrefix, loadDocumentEntity);

            HtmlViewOptions viewOptions = HtmlViewOptions.ForExternalResources(streamFactory, streamFactory);
            viewOptions.SpreadsheetOptions = SpreadsheetOptions.ForOnePagePerSheet();
            viewOptions.SpreadsheetOptions.RenderGridLines = true;

            ViewInfoOptions viewInfoOptions = ViewInfoOptions.FromHtmlViewOptions(viewOptions);

            using (GroupDocs.Viewer.Viewer viewer = new GroupDocs.Viewer.Viewer(documentGuid, loadOptions))
            {
                ViewInfo viewInfo = viewer.GetViewInfo(viewInfoOptions);
                viewer.View(viewOptions);
                foreach (Page page in viewInfo.Pages)
                {
                    if (page.Number <= loadDocumentEntity.GetPages().Count())
                    {
                        loadDocumentEntity.GetPages()[page.Number - 1].number = page.Number;
                        loadDocumentEntity.GetPages()[page.Number - 1].angle = 0;
                        loadDocumentEntity.GetPages()[page.Number - 1].height = page.Height;
                        loadDocumentEntity.GetPages()[page.Number - 1].width = page.Width;
                        loadDocumentEntity.GetPages()[page.Number - 1].sheetName = page.Name;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            loadDocumentEntity.SetGuid(documentGuid);
            return loadDocumentEntity;
        }
    }
}