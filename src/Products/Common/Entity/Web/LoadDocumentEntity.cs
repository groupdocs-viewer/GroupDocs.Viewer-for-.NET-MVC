using Newtonsoft.Json;
using System.Collections.Generic;

namespace GroupDocs.Viewer.MVC.Products.Common.Entity.Web
{
    public class LoadDocumentEntity
    {
        ///Document Guid
        [JsonProperty]
        private string guid;

        ///List of pages
        [JsonProperty]
        private List<PageDescriptionEntity> pages = new List<PageDescriptionEntity>();

        ///Print allowed
        [JsonProperty]
        private bool printAllowed = true;

        ///Show grid lines
        [JsonProperty]
        private bool showGridLines = true;

        public void SetPrintAllowed(bool allowed)
        {
            this.printAllowed = allowed;
        }

        public bool GetPrintAllowed()
        {
            return this.printAllowed;
        }

        public void SetShowGridLines(bool show)
        {
            this.showGridLines = show;
        }

        public bool GetShowGridLines()
        {
            return this.showGridLines;
        }

        public void SetGuid(string guid)
        {
            this.guid = guid;
        }

        public string GetGuid()
        {
            return this.guid;
        }

        public void SetPages(PageDescriptionEntity page)
        {
            this.pages.Add(page);
        }

        public List<PageDescriptionEntity> GetPages()
        {
            return this.pages;
        }
    }
}