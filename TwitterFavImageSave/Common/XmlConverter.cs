﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace TwitterFavImageSave.Common
{
    /// <summary>
    /// XMLシリアライザ／デシリアライザ
    /// </summary>
    public static class XmlConverter
    {
        /// <summary>
        /// 任意のオブジェクトをxmlにシリアル化する
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="obj">オブジェクト</param>
        /// <param name="Path">ファイルパス</param>
        /// <returns>成功したらtrue、失敗したらfalse</returns>
        public static bool Serialize<T>(T obj, string Path) where T : class
        {
            bool ret = false;
            try
            {
                using (var fs = new StreamWriter(Path, false, Encoding.GetEncoding("utf-8")))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(fs, obj);
                    ret = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return ret;
        }

        /// <summary>
        /// 任意のオブジェクトをxmlにシリアル化する
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="obj">オブジェクト</param>
        /// <param name="Path">ファイルパス</param>
        /// <returns>成功したらtrue、失敗したらfalse</returns>
        public static bool SerializeFromCol<T>(ObservableCollection<T> obj, string Path) where T : class
        {
            bool ret = false;
            try
            {
                using (var fs = new StreamWriter(Path, false, Encoding.GetEncoding("utf-8")))
                {
                    var serializer = new XmlSerializer(typeof(ObservableCollection<T>));
                    serializer.Serialize(fs, obj);
                    ret = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return ret;
        }

        /// <summary>
        /// xmlファイルを任意のオブジェクトに逆シリアル化する
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="Path">ファイルパス</param>
        /// <returns>オブジェクト</returns>
        public static T DeSerialize<T>(string Path) where T : class
        {
            var ret = default(T);
            try
            {
                using (var fs = new StreamReader(Path, Encoding.GetEncoding("utf-8")))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    ret = (T)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return ret;
        }

        /// <summary>
        /// xmlファイルを任意のオブジェクトに逆シリアル化する
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="Path">ファイルパス</param>
        /// <returns>オブジェクト</returns>
        public static ObservableCollection<T> DeSerializeToCol<T>(string Path) where T : class
        {
            var ret = default(ObservableCollection<T>);
            try
            {
                using (var fs = new StreamReader(Path, Encoding.GetEncoding("utf-8")))
                {
                    var serializer = new XmlSerializer(typeof(ObservableCollection<T>));
                    ret = (ObservableCollection<T>)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return ret;
        }
    }
}
