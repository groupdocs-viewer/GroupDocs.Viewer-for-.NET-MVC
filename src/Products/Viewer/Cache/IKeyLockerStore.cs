namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    interface IKeyLockerStore
    {
        object GetLockerFor(string key);
    }
}
