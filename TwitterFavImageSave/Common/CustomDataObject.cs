using System;
using System.Windows.Forms;

namespace TwitterFavImageSave.Common
{
    public class CustomDataObject : DataObject
    {
        public Func<string, bool, object> OnGetData;

        public override object GetData(string format, bool autoConvert)
        {
            return OnGetData(format, autoConvert) ?? base.GetData(format, autoConvert);
        }
    }
}
