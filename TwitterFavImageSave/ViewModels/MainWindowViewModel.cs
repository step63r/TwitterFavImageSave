using CoreTweet;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.Resources;
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
        private OAuth.OAuthSession _session;
        private DelegateCommand _cmdWindowClosing;

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
            // トークン読込
            if (string.IsNullOrEmpty(Properties.Settings.Default.AccessToken) || string.IsNullOrEmpty(Properties.Settings.Default.AccessTokenSecret))
            {
                // 存在しなければ認証開始
                StartAuthorize();
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
        public async void DispatchDialogOk()
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
        /// Windowを閉じる時のイベントハンドラ
        /// </summary>
        public void ExecuteWindowClosing()
        {
            Properties.Settings.Default.Save();
        }
        private bool CanExecuteWindowClosing()
        {
            return true;
        }
    }
}
