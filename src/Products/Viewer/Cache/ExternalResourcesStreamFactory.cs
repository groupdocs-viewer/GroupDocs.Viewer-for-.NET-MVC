using GroupDocs.Viewer.Interfaces;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.Results;
using System.IO;
using System.Text;
using WebMarkupMin.Core;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    public class ExternalResourcesStreamFactory : IPageStreamFactory, IResourceStreamFactory
    {
        private readonly string _outputPath;
        private readonly string _urlPrefix;
        private readonly LoadDocumentEntity _loadDocumentEntity;

        public ExternalResourcesStreamFactory(string outputPath, string urlPrefix, LoadDocumentEntity loadDocumentEntity)
        {
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            _outputPath = outputPath;
            _urlPrefix = urlPrefix;
            _loadDocumentEntity = loadDocumentEntity;
        }

        public Stream CreatePageStream(int pageNumber) =>
            new MemoryStream();

        public void ReleasePageStream(int pageNumber, Stream pageStream)
        {

            byte[] bytes = null;
            using (MemoryStream memoryStream = (MemoryStream)pageStream)
            {
                memoryStream.Position = 0;
                bytes = memoryStream.ToArray();
            }

            string html = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            var result = new HtmlMinifier().Minify(html);

            File.WriteAllText(Path.Combine(_outputPath, $"p_{pageNumber}.html"), result.MinifiedContent);
            //if (string.IsNullOrEmpty(_urlPrefix))
            //{
            //    if (pageNumber == 1)
            //    {
            //        _loadDocumentEntity.PageStreamForThumbnail = bytes;
            //    }
            //}
            //else
            {
                PageDescriptionEntity pageData = new PageDescriptionEntity();
                pageData.SetData(result.MinifiedContent);
                _loadDocumentEntity.SetPages(pageData);
            }
        }

        public Stream CreateResourceStream(int pageNumber, Resource resource) =>
            File.OpenWrite(Path.Combine(_outputPath, $"p_{pageNumber}_{resource.FileName}"));

        public string CreateResourceUrl(int pageNumber, Resource resource)
        {
            return $"{_urlPrefix}/p_{ pageNumber}_{ resource.FileName}";
        }

        public void ReleaseResourceStream(int pageNumber, Resource resource, Stream resourceStream) =>
            resourceStream.Dispose();
    }
}
