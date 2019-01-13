using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterFavImageSave.Common
{
    /// <summary>
    /// 共通パス設定
    /// </summary>
    public static class CommonPath
    {
        /// <summary>
        /// 共通設定ディレクトリ
        /// </summary>
        public static string BaseDIr = string.Format(@"{0}\TwitterFavImageSave", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

        /// <summary>
        /// Token情報
        /// </summary>
        public static string AuthInfo = string.Format(@"{0}\AuthInfo.xml", BaseDIr);
    }

    /// <summary>
    /// ダイアログ種別
    /// </summary>
    public enum DialogType
    {
        StartAuth,
        InputPincode,
        RemoveAuth,
        License
    }
}
