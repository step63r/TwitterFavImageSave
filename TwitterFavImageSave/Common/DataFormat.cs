﻿using System;
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
        public static string BaseDir = string.Format(@"{0}\TwitterFavImageSave", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        /// <summary>
        /// 一時ファイル格納ディレクトリ
        /// </summary>
        public static string TmpDir = string.Format(@"{0}\tmp", BaseDir);
    }

    public static class CommonSettings
    {
        /// <summary>
        /// 一度に読むTweet数
        /// </summary>
        public static int ReadCount = 50;
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
