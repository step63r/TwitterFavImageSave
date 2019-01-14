using CoreTweet;
using CoreTweet.Core;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Resources;
using System.Windows.Controls;
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
        private OAuth.OAuthSession _session;
        private DelegateCommand _cmdWindowClosing;
        private DelegateCommand<object> _cmdOnScrollChanged;
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
        /// Twitterセッションオブジェクト
        /// </summary>
        public OAuth.OAuthSession Session
        {
            get { return _session; }
            set { SetProperty(ref _session, value); }
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

        public MainWindowViewModel()
        {
            AccessToken = new Tokens();
            //Properties.Settings.Default.AccessToken = "";
            //Properties.Settings.Default.AccessTokenSecret = "";
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
            }

            // TODO: 異常系
            var ret = GetFavorites();
            if (ret is null)
            {
                // お気に入りに何も登録されていなかった場合
                // TODO
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExecuteScrollChanged(object obj)
        {
            var tuple = obj as Tuple<double, double>;
            if (tuple.Item1 == tuple.Item2)
            {
                var ret = GetFavorites();
                if (ret is null)
                {
                    return;
                }

                LastTweetId = GetOldestTwitterId(ret);

                foreach (var sts in ret)
                {
                    bool bHasMedia = sts.ExtendedEntities.Media is null ? false : true;
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
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CanExecuteScrollChanged()
        {
            return true;
        }
    }
}
