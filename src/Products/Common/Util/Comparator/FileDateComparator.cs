using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Total.MVC.Products.Common.Util.Comparator
{
    /// <summary>
    /// FileDateComparator
    /// </summary>
    public class FileDateComparator : IComparer<string>
    {
        /// <summary>
        /// Compare file creation dates
        /// </summary>
        /// <param name="x">string</param>
        /// <param name="y">string</param>
        /// <returns></returns>
        public int Compare(string x, string y)
        {
            string strExt1 = File.GetCreationTime(x).ToString();
            string strExt2 = File.GetCreationTime(y).ToString();

            if (strExt1.Equals(strExt2))
            {
                return x.CompareTo(y);
            }
            else
            {
                return strExt1.CompareTo(strExt2);
            }
        }
    }
}