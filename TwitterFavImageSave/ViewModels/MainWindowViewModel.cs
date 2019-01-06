using CoreTweet;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Diagnostics;

namespace TwitterFavImageSave.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "TwitterFavImageSave";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private DelegateCommand _firstCommand;
        public DelegateCommand FirstCommand
        {
            get { return _firstCommand = _firstCommand ?? new DelegateCommand(GetAuthorize); }
        }

        private DelegateCommand _secondCommand;
        public DelegateCommand SecondCommand
        {
            get { return _secondCommand = _secondCommand ?? new DelegateCommand(Tweet, CanTweet); }
        }

        private string _pincode = "";
        public string Pincode
        {
            get { return _pincode; }
            set
            {
                SetProperty(ref _pincode, value);
                SecondCommand.RaiseCanExecuteChanged();
            }
        }

        private string _message = "";
        public string Message
        {
            get { return _message; }
            set
            {
                SetProperty(ref _message, value);
                SecondCommand.RaiseCanExecuteChanged();
            }
        }

        private OAuth.OAuthSession _session;
        public OAuth.OAuthSession Session
        {
            get { return _session; }
            set { SetProperty(ref _session, value); }
        }

        public MainWindowViewModel()
        {
        }

        private void GetAuthorize()
        {
            // TODO: Input here API Token and Secret Token
            Session = OAuth.Authorize("", "");
            Process.Start(Session.AuthorizeUri.ToString());
        }

        private void Tweet()
        {
            var t = Session.GetTokens(Pincode);
            var ret = t.Statuses.Update(status => Message);
        }

        private bool CanTweet()
        {
            return !string.IsNullOrEmpty(Pincode) && !string.IsNullOrEmpty(Message);
        }
    }
}
