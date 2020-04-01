using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    interface IKeyLockerStore
    {
        object GetLockerFor(string key);
    }
}
