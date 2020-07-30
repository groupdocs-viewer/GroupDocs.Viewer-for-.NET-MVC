using GroupDocs.Viewer.Interfaces;
using GroupDocs.Viewer.Results;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    public class ExternalResourcesStreamFactory : IPageStreamFactory, IResourceStreamFactory
    {
        // Resources storage path.
        private readonly string _outputPath;

        // URL for resources download.
        private readonly string _urlPrefix;

        public ExternalResourcesStreamFactory(string cachePath, string resourcesPath)
        {
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            _outputPath = cachePath;
            _urlPrefix = resourcesPath;
        }

        public Stream CreatePageStream(int pageNumber) =>
            File.OpenWrite(Path.Combine(_outputPath, $"p_{pageNumber}.html"));

        public void ReleasePageStream(int pageNumber, Stream pageStream) =>
            pageStream.Dispose();

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