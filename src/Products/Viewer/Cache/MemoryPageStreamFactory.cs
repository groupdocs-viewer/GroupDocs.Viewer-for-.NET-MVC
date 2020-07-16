using GroupDocs.Viewer.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    /// <summary>
    /// Produces pages streams.
    /// </summary>
    internal class MemoryPageStreamFactory : IPageStreamFactory
    {
        private readonly List<MemoryStream> pages;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryPageStreamFactory"/> class.
        /// </summary>
        /// <param name="pages">List of pages memory streams.</param>
        public MemoryPageStreamFactory(List<MemoryStream> pages)
        {
            this.pages = pages;
        }

        /// <summary>
        /// Creates page stream.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <returns>Page stream.</returns>
        public Stream CreatePageStream(int pageNumber)
        {
            MemoryStream pageStream = new MemoryStream();

            this.pages.Add(pageStream);

            return pageStream;
        }

        /// <summary>
        /// Releases page stream.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="pageStream">Page stream.</param>
        public void ReleasePageStream(int pageNumber, Stream pageStream)
        {
            // Do not release page stream as we'll need to keep the stream open
        }
    }
}