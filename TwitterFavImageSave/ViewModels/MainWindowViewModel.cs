using CoreTweet;
using CoreTweet.Core;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TwitterFavImageSave.Common;
using TwitterFavImageSave.UserControls;

namespace TwitterFavImageSave.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region コマンド・プロパティ
        private string _title = "TwitterFavImageSave";
        private Tokens _accessToken;
        private string _pincode = "";
        private long _lastTweetId;
        private bool _isLoadingUserControl = false;
        private OAuth.OAuthSession _session;
        private DelegateCommand _cmdWindowLoaded;
        private DelegateCommand _cmdWindowClosing;
        private DelegateCommand<object> _cmdOnScrollChanged;
        private DelegateCommand _btnRemoveAccessToken;
        private TweetObjectUserControlViewModel _objectViewModel;
        
        /// <summary>
        /// MainWindowタイトル
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        /// <summary>
        /// アクセストークン
        /// </summary>
        public Tokens AccessToken
        {
            get { return _accessToken; }
            set
            {
                SetProperty(ref _accessToken, value);
                BtnRemoveAccessToken.RaiseCanExecuteChanged();
            }
        }
        /// <summary>
        /// PINコード
        /// </summary>
        public string Pincode
        {
            get { return _pincode; }
            set
            {
                SetProperty(ref _pincode, value);
                DispatchDialogOkCommand.RaiseCanExecuteChanged();
            }
        }
        /// <summary>
        /// 最後に取得した最も古いTweet ID
        /// </summary>
        public long LastTweetId
        {
            get { return _lastTweetId; }
            set
            {
                SetProperty(ref _lastTweetId, value);
            }
        }
        /// <summary>
        /// UserControl更新中フラグ
        /// </summary>
        public bool IsLoadingUserControl
        {
            get { return _isLoadingUserControl; }
            set
            {
                SetProperty(ref _isLoadingUserControl, value);
            }
        }
        /// <summary>
        /// Twitterセッションオブジェクト
        /// </summary>
        public OAuth.OAuthSession Session
        {
            get { return _session; }
            set { SetProperty(ref _session, value); }
        }
        /// <summary>
        /// WindowのLoadが完了した時のイベントコマンド
        /// </summary>
        public DelegateCommand CmdWindowLoaded
        {
            get { return _cmdWindowLoaded = _cmdWindowLoaded ?? new DelegateCommand(ExecuteWindowLoaded, CanExecuteWindowLoaded); }
        }
        /// <summary>
        /// Windowが閉じる時のイベントコマンド
        /// </summary>
        public DelegateCommand CmdWindowClosing
        {
            get { return _cmdWindowClosing = _cmdWindowClosing ?? new DelegateCommand(ExecuteWindowClosing, CanExecuteWindowClosing); }
        }        
        /// <summary>
        /// ScrollChangedイベントコマンド
        /// </summary>
        public DelegateCommand<object> CmdOnScrollChanged
        {
            get { return _cmdOnScrollChanged = _cmdOnScrollChanged ?? new DelegateCommand<object>(ExecuteScrollChanged); }
        }
        /// <summary>
        /// 「ログアウト」ボタン
        /// </summary>
        public DelegateCommand BtnRemoveAccessToken
        {
            get { return _btnRemoveAccessToken = _btnRemoveAccessToken ?? new DelegateCommand(ExecuteRemoveAccessToken, CanExecuteRemoveAccessToken); }
        }
        public ObservableCollection<TweetObjectUserControlViewModel> TweetList { get; set; } = new ObservableCollection<TweetObjectUserControlViewModel>();
        public TweetObjectUserControlViewModel ObjectViewModel
        {
            get { return _objectViewModel; }
            set { SetProperty(ref _objectViewModel, value); }
        }        
        #region Dialog系
        private DelegateCommand _dispatchDialogOkCommand;
        private DelegateCommand _dispatchDialogCancelCommand;
        private DialogType _dType;
        private string _message;
        private object _dialogView;
        private bool _isDialogOpen;

        /// <summary>
        /// ダイアログで「OK」が押下されたときの挙動
        /// </summary>
        public DelegateCommand DispatchDialogOkCommand
        {
            get { return _dispatchDialogOkCommand = _dispatchDialogOkCommand ?? new DelegateCommand(DispatchDialogOk, CanDispatchDialogOk); }
        }
        /// <summary>
        /// ダイアログで「Cancel」が押下されたときの挙動
        /// </summary>
        public DelegateCommand DispatchDialogCancelCommand
        {
            get { return _dispatchDialogCancelCommand = _dispatchDialogCancelCommand ?? new DelegateCommand(DispatchDialogCancel, CanDispatchDialogCancel); }
        }
        /// <summary>
        /// 開いているダイアログ種別
        /// </summary>
        public DialogType DType
        {
            get
            {
                return _dType;
            }
            set
            {
                _dType = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// ダイアログメッセージ
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// アクティブなDialogView
        /// </summary>
        public object DialogView
        {
            get
            {
                return _dialogView;
            }
            set
            {
                _dialogView = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Dialogが表示中かどうか
        /// </summary>
        public bool IsDialogOpen
        {
            get
            {
                return _isDialogOpen;
            }
            set
            {
                _isDialogOpen = value;
                RaisePropertyChanged();
            }
        }
        #endregion
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel()
        {
            // 初期処理
            GenerateCommonPath();
        }

        /// <summary>
        /// WindowのLoadが完了した時のイベントハンドラ
        /// </summary>
        public void ExecuteWindowLoaded()
        {
            AccessToken = new Tokens();
            BindingOperations.EnableCollectionSynchronization(TweetList, new object());

            // トークン読込
            if (string.IsNullOrEmpty(Properties.Settings.Default.AccessToken) || string.IsNullOrEmpty(Properties.Settings.Default.AccessTokenSecret))
            {
                // 存在しなければ認証開始
                StartAuthorize();
            }
            else
            {
                AccessToken = Tokens.Create(
                    Properties.Settings.Default.TwitterApiKey,
                    Properties.Settings.Default.TwitterApiSecret,
                    Properties.Settings.Default.AccessToken,
                    Properties.Settings.Default.AccessTokenSecret);

                var t = UpdateUserControl();
            }
        }
        private bool CanExecuteWindowLoaded()
        {
            return true;
        }

        /// <summary>
        /// 画像リストを更新する
        /// </summary>
        private async Task UpdateUserControl()
        {
            IsLoadingUserControl = true;
            await Task.Run(() =>
            {
                var ret = GetFavorites();
                if (ret is null)
                {
                    // お気に入りに何も登録されていなかった場合
                    return;
                }

                LastTweetId = GetOldestTwitterId(ret);

                if (LastTweetId > 0)
                {
                    foreach (var sts in ret)
                    {
                        bool bHasMedia = sts.ExtendedEntities is null ? false : true;
                        if (bHasMedia)
                        {
                            for (int i = 0; i < sts.ExtendedEntities.Media.Length; i++)
                            {
                                var item = new TweetObjectUserControlViewModel(sts, i);
                                TweetList.Add(item);
                            }
                        }
                    }
                }
            }).ConfigureAwait(false);
            IsLoadingUserControl = false;
        }

        /// <summary>
        /// Twitter認証開始ダイアログを表示する
        /// </summary>
        public void StartAuthorize()
        {
            Message = "ブラウザを開き Twitter ログイン認証を開始します。";
            DType = DialogType.StartAuth;
            DialogView = new MaterialDialogOk()
            {
                DataContext = this
            };
            IsDialogOpen = true;
        }

        /// <summary>
        /// 「ログアウト」ボタンのイベントハンドラ
        /// </summary>
        public void ExecuteRemoveAccessToken()
        {
            Message = "認証情報を消去し、画面をリフレッシュします";
            DType = DialogType.RemoveAuth;
            DialogView = new MaterialDialogOkCancel()
            {
                DataContext = this
            };
            IsDialogOpen = true;
        }
        private bool CanExecuteRemoveAccessToken()
        {
            return !(string.IsNullOrEmpty(Properties.Settings.Default.AccessToken) || string.IsNullOrEmpty(Properties.Settings.Default.AccessTokenSecret));
        }

        /// <summary>
        /// ダイアログで「OK」が押下されたときの挙動をディスパッチする
        /// </summary>
        public void DispatchDialogOk()
        {
            // ダイアログ種別で分岐
            switch (DType)
            {
                case DialogType.StartAuth:
                    // 認証開始
                    GetAuthorize();
                    IsDialogOpen = false;
                    DialogView = null;

                    // PINコード入力
                    DType = DialogType.InputPincode;
                    DialogView = new DialogInputPincode()
                    {
                        DataContext = this
                    };
                    IsDialogOpen = true;
                    break;

                case DialogType.InputPincode:
                    // アクセストークンを保存
                    // TODO: 異常系
                    AccessToken = Session.GetTokens(Pincode);
                    Properties.Settings.Default.AccessToken = AccessToken.AccessToken;
                    Properties.Settings.Default.AccessTokenSecret = AccessToken.AccessTokenSecret;

                    // ダイアログを閉じる
                    IsDialogOpen = false;
                    DialogView = null;
                    var t = UpdateUserControl();
                    break;

                case DialogType.RemoveAuth:
                    // アクセストークンを削除
                    Properties.Settings.Default.AccessToken = "";
                    Properties.Settings.Default.AccessTokenSecret = "";
                    
                    // ダイアログを閉じる
                    IsDialogOpen = false;
                    DialogView = null;

                    // 画面リフレッシュ
                    Refresh();
                    ExecuteWindowLoaded();
                    break;
            }
        }
        private bool CanDispatchDialogOk()
        {
            bool ret = false;
            // ダイアログ種別で分岐
            switch (DType)
            {
                case DialogType.StartAuth:
                    ret = true;
                    break;

                case DialogType.InputPincode:
                    ret = !string.IsNullOrEmpty(Pincode);
                    break;

                case DialogType.RemoveAuth:
                    ret = true;
                    break;
            }
            return ret;
        }

        /// <summary>
        /// ダイアログで「Cancel」が押下されたときの挙動をディスパッチする
        /// </summary>
        public void DispatchDialogCancel()
        {
            IsDialogOpen = false;
            DialogView = null;
        }
        private bool CanDispatchDialogCancel()
        {
            return true;
        }

        /// <summary>
        /// Twitter認証を行う
        /// </summary>
        private void GetAuthorize()
        {
            Session = OAuth.Authorize(Properties.Settings.Default.TwitterApiKey, Properties.Settings.Default.TwitterApiSecret);
            Process.Start(Session.AuthorizeUri.ToString());
        }

        /// <summary>
        /// お気に入りを取得する
        /// </summary>
        /// <returns>お気に入りしたTweet</returns>
        private ListedResponse<Status> GetFavorites()
        {
            ListedResponse<Status> ret = null;
            if (LastTweetId == 0)
            {
                ret = AccessToken.Favorites.List(count => CommonSettings.ReadCount);
            }
            else if (LastTweetId > 0)
            {
                ret = AccessToken.Favorites.List(count => CommonSettings.ReadCount, max_id => LastTweetId - 1);
            }
            return ret;
        }

        /// <summary>
        /// 最も古いTweet IDを取得する（リストに何もない場合は -1 を返す）
        /// </summary>
        /// <param name="t"></param>
        /// <returns>最も古いTwitter ID</returns>
        private long GetOldestTwitterId(ListedResponse<Status> t)
        {
            long ret = 0;

            if (t.Count == 0)
            {
                ret = -1;
            }

            foreach (var sts in t)
            {
                if (ret == 0)
                {
                    ret = sts.Id;
                }
                else
                {
                    if (ret > sts.Id)
                    {
                        ret = sts.Id;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Windowを閉じる時のイベントハンドラ
        /// </summary>
        public void ExecuteWindowClosing()
        {
            Properties.Settings.Default.Save();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CanExecuteWindowClosing()
        {
            return true;
        }

        /// <summary>
        /// ScrollChangedイベントハンドラ
        /// </summary>
        /// <param name="obj"></param>
        public void ExecuteScrollChanged(object obj)
        {
            if (IsLoadingUserControl)
            {
                return;
            }

            var tuple = obj as Tuple<double, double>;
            if (tuple.Item1 == tuple.Item2)
            {
                var t = UpdateUserControl();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CanExecuteScrollChanged()
        {
            return true;
        }

        /// <summary>
        /// 共通パスを全部作る（既に存在する場合は無視する）
        /// </summary>
        private void GenerateCommonPath()
        {
            // 後でちゃんと考える
            if (!Directory.Exists(CommonPath.TmpDir))
            {
                Directory.CreateDirectory(CommonPath.TmpDir);
            }
        }

        private void Refresh()
        {
            AccessToken = null;
            Pincode = "";
            LastTweetId = 0;
            IsLoadingUserControl = false;
            Session = null;
            TweetList.Clear();
            //TweetList = new ObservableCollection<TweetObjectUserControlViewModel>();
        }
    }
}
