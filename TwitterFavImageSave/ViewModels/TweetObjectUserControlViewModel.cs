using CoreTweet;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TwitterFavImageSave.Common;
using TwitterFavImageSave.UserControls;

namespace TwitterFavImageSave.ViewModels
{
    public class TweetObjectUserControlViewModel : BindableBase
    {
        #region コマンド・プロパティ
        private Status _status;
        private string _tweetText;
        private string _userName;
        private string _userId;
        private string _tweetUrl;
        private BitmapImage _tweetImage;
        private BitmapImage _userImage;
        private string _userNameDisplay;
        private DelegateCommand _cmdSaveImageAs;
        private DelegateCommand _cmdViewInBrowser;
        private DelegateCommand<object> _cmdMouseMove;

        /// <summary>
        /// TweetのStatusオブジェクト
        /// </summary>
        public Status Status
        {
            get { return _status; }
            set
            {
                SetProperty(ref _status, value);
                CmdViewInBrowser.RaiseCanExecuteChanged();
            }
        }
        /// <summary>
        /// ツイート内容
        /// </summary>
        public string TweetText
        {
            get { return _tweetText; }
            set { SetProperty(ref _tweetText, value); }
        }
        /// <summary>
        /// ユーザ名
        /// </summary>
        public string UserName
        {
            get { return _userName; }
            set { SetProperty(ref _userName, value); }
        }
        /// <summary>
        /// ユーザID
        /// </summary>
        public string UserId
        {
            get { return _userId; }
            set { SetProperty(ref _userId, value); }
        }
        /// <summary>
        /// TweetのURL
        /// </summary>
        public string TweetUrl
        {
            get { return _tweetUrl; }
            set { SetProperty(ref _tweetUrl, value); }
        }
        /// <summary>
        /// Tweet中の画像
        /// </summary>
        public BitmapImage TweetImage
        {
            get { return _tweetImage; }
            set { SetProperty(ref _tweetImage, value); }
        }
        /// <summary>
        /// ユーザプロフィール画像
        /// </summary>
        public BitmapImage UserImage
        {
            get { return _userImage; }
            set { SetProperty(ref _userImage, value); }
        }
        /// <summary>
        /// 表示するユーザ名（[UserName] @ScreenName）
        /// </summary>
        public string UserNameDisplay
        {
            get { return _userNameDisplay; }
            set { SetProperty(ref _userNameDisplay, value); }
        }
        /// <summary>
        /// 画像を保存コマンド
        /// </summary>
        public DelegateCommand CmdSaveImageAs
        {
            get { return _cmdSaveImageAs = _cmdSaveImageAs ?? new DelegateCommand(ExecuteSaveImageAs, CanExecuteSaveImageAs); }
        }
        /// <summary>
        /// ブラウザで開くコマンド
        /// </summary>
        public DelegateCommand CmdViewInBrowser
        {
            get { return _cmdViewInBrowser = _cmdViewInBrowser ?? new DelegateCommand(ExecuteViewInBrowser, CanExecuteViewInBrowser); }
        }
        /// <summary>
        /// MouseMoveイベントコマンド
        /// </summary>
        public DelegateCommand<object> CmdMouseMove
        {
            //get { return _cmdMouseMove = _cmdMouseMove ?? new DelegateCommand(ExecuteMouseMove, CanExecuteMouseMove); }
            //get { return _cmdMouseMove = _cmdMouseMove ?? new DelegateCommand<object>(param => ExecuteMouseMove((MouseEventArgs)param)); }
            get { return _cmdMouseMove = _cmdMouseMove ?? new DelegateCommand<object>(ExecuteMouseMove); }

        }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TweetObjectUserControlViewModel()
        {

        }

        /// <summary>
        /// 引数付きコンストラクタ
        /// </summary>
        /// <param name="obj">Statusオブジェクト</param>
        /// <param name="mediaIndex">メディアのインデックス</param>
        public TweetObjectUserControlViewModel(Status obj, int mediaIndex)
        {
            Status = obj;

            TweetText = Status.Text;
            UserName = Status.User.Name;
            UserId = Status.User.ScreenName;
            TweetImage = new BitmapImage(new System.Uri(Status.ExtendedEntities.Media[mediaIndex].MediaUrl));
            UserImage = new BitmapImage(new System.Uri(Status.User.ProfileImageUrl));
            UserNameDisplay = CreateUserNameDisplay(UserName, UserId);
        }

        private string CreateUserNameDisplay(string name, string id)
        {
            return string.Format("{0} @{1}", name, id);
        }

        /// <summary>
        /// ブラウザで開く
        /// </summary>
        public void ExecuteViewInBrowser()
        {
            string url = string.Format(@"https://twitter.com/{0}/status/{1}", Status.User.Id, Status.Id);
            Process.Start(url);
        }
        /// <summary>
        /// ブラウザで開くコマンドの実行可能ステータス
        /// </summary>
        /// <returns></returns>
        private bool CanExecuteViewInBrowser()
        {
            return !(Status is null);
        }

        /// <summary>
        /// 画像を保存
        /// </summary>
        public void ExecuteSaveImageAs()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.Filter = "PNG画像(.png)|*.png|JPG画像(.jpg)|*.jpg|すべてのファイル(*.*)|*.*";
            saveFileDialog.FileName = TweetImage.UriSource.Segments[2];
            bool? ret = saveFileDialog.ShowDialog();
            if (ret == true)
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(GenerateOriginalUrl(TweetImage.UriSource.AbsoluteUri), Path.GetFullPath(saveFileDialog.FileName));
                }
            }

        }
        /// <summary>
        /// 画像を保存コマンドの実行可能ステータス
        /// </summary>
        /// <returns></returns>
        private bool CanExecuteSaveImageAs()
        {
            return !(Status is null);
        }

        public bool IsProcessing = false;

        /// <summary>
        /// MouseMoveイベントハンドラ
        /// </summary>
        public void ExecuteMouseMove(object obj)
        {
            var e = obj as IInputElement;
            if (!(e is null))
            {
                string src_path = GenerateOriginalUrl(TweetImage.UriSource.AbsoluteUri);
                string dst_path = string.Format(@"{0}\{1}", CommonPath.TmpDir, TweetImage.UriSource.Segments[2]);

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    // 処理中フラグがOFFの場合、ファイルをダウンロードしてフラグを立てる
                    if (!IsProcessing)
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(src_path, dst_path);
                        }
                        IsProcessing = true;
                    }

                    //string path = Path.GetFullPath(dst_path);
                    //string path = GenerateOriginalUrl(TweetImage.UriSource.AbsoluteUri);
                    string[] paths = { dst_path };

                    var dataObject = new DataObject();
                    dataObject.SetData(DataFormats.FileDrop, paths);

                    // あまり良い方法ではない
                    var ret = DragDrop.DoDragDrop((TweetObjectUserControl)e, dataObject, DragDropEffects.Copy);

                    // コピーが完了したら元ファイルを消す
                    if (ret == DragDropEffects.Copy)
                    {
                        if (IsProcessing)
                        {
                            if (File.Exists(Path.GetFullPath(dst_path)))
                            {
                                File.Delete(Path.GetFullPath(dst_path));
                            }
                            IsProcessing = false;
                        }
                    }

                    //string originalpath = GenerateOriginalUrl(TweetImage.UriSource.AbsoluteUri);
                    //string dstpath = TweetImage.UriSource.Segments[2];
                    //byte[] data = null;
                    //var dobj = new CustomDataObject();
                    //dobj.SetData("FileGroupDescriptorW", null);
                    //dobj.SetData("FileContents", null);
                    //dobj.OnGetData = (format, autoConvert) =>
                    //{
                    //    if (data == null)
                    //    {
                    //        using (var webClient = new WebClient())
                    //        {
                    //            data = webClient.DownloadData(originalpath);
                    //        }
                    //    }
                    //    switch (format)
                    //    {
                    //        case "FileGroupDescriptorW":
                    //            var ms = new MemoryStream();
                    //            var bw = new BinaryWriter(ms);
                    //            bw.Write(1);
                    //            bw.Write(0x4040);
                    //            bw.Write(new byte[16 + 4 * 5 + 8 * 3 + 4]);
                    //            bw.Write(data.Length);
                    //            bw.Write(File.ReadAllBytes(dstpath));
                    //            //bw.Write(Encoding.Unicode.GetBytes("TestDnD.txt".PadRight(260, '\0')));
                    //            return ms;
                    //        case "FileContents":
                    //            return new MemoryStream(data);
                    //    }
                    //    return null;
                    //};
                    //var dde = DragDrop.DoDragDrop((TweetObjectUserControl)e, dobj, DragDropEffects.Copy);
                }
                //// ドラッグ解除
                //else if (Mouse.LeftButton == MouseButtonState.Released)
                //{
                //    if (IsProcessing)
                //    {
                //        if (File.Exists(Path.GetFullPath(dst_path)))
                //        {
                //            File.Delete(Path.GetFullPath(dst_path));
                //        }
                //        IsProcessing = false;
                //    }
                //}
            }
        }

        private string GenerateOriginalUrl(string url)
        {
            return string.Format(@"{0}:orig", url);
        }
    }
}
