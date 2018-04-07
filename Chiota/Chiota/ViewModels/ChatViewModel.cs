﻿namespace Chiota.ViewModels
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Globalization;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows.Input;

  using Chiota.IOTAServices;
  using Chiota.Messages;
  using Chiota.Models;
  using Chiota.Services;

  using Tangle.Net.Entity;
  using Tangle.Net.Utils;

  using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;

  using Xamarin.Forms;

  public class ChatViewModel : BaseViewModel
  {
    public Action DisplayMessageTooLong;

    public Action DisplayInvalidPublicKeyPrompt;

    private readonly User user;

    private readonly Contact contact;

    private readonly NtruKex ntruKex;

    private readonly ListView messagesListView;

    private string outgoingText;

    private ObservableCollection<MessageViewModel> messagesList;

    public ChatViewModel(ListView messagesListView, Contact contact, User user)
    {
      this.ntruKex = new NtruKex();
      this.Messages = new ObservableCollection<MessageViewModel>();
      this.user = user;
      this.contact = contact;
      this.messagesListView = messagesListView;
      this.PageIsShown = true;
      this.OutGoingText = null;

      // reset hash short storage, because it's different for every chat
      this.user.TangleMessenger.ShorStorageHashes = new List<Hash>();

      this.SendCommand = new Command(async () => { await this.SendMessage(); });
    }

    public bool PageIsShown { get; set; }

    public string OutGoingText
    {
      get => this.outgoingText;
      set
      {
        this.outgoingText = value;
        this.RaisePropertyChanged();
      }
    }

    public ICommand SendCommand { get; set; }

    public ObservableCollection<MessageViewModel> Messages
    {
      get => this.messagesList;
      set
      {
        this.messagesList = value;
        this.RaisePropertyChanged();
      }
    }

    public async void OnAppearing()
    {
      // cancel if there is no interent
      this.contact.PublicNtruKey = await this.GetContactPublicKey();
      if (this.contact.PublicNtruKey == null)
      {
        // todo: delete contact
        this.DisplayInvalidPublicKeyPrompt();
        await this.Navigation.PopAsync();
      }
      else
      {
        this.GetMessagesAsync(this.Messages);
      }

      var messagestart = new StartLongRunningTaskMessage();
      MessagingCenter.Send(messagestart, "StopLongRunningTaskMessage");
    }

    public void OnDisappearing()
    {
      var messagestart = new StartLongRunningTaskMessage();
      MessagingCenter.Send(messagestart, "StartLongRunningTaskMessage");
    }

    private async Task<IAsymmetricKey> GetContactPublicKey()
    {
      var trytes = await this.user.TangleMessenger.GetMessagesAsync(this.contact.PublicKeyAdress, 3);
      var contactInfos = IotaHelper.GetPublicKeysAndContactAddresses(trytes);
      if (contactInfos == null || contactInfos.Count > 1)
      {
        return null;
      }

      return contactInfos[0].PublicNtruKey;
    }

    private async Task SendMessage()
    {
      this.IsBusy = true;

      // No json object, because of the 106 character limit
      if (this.OutGoingText.Length > 105)
      {
        this.DisplayMessageTooLong();
      }
      else
      {
        var trytesDate = TryteString.FromUtf8String(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

        // helps to identify who send the message
        var signature = this.user.PublicKeyAddress.Substring(0, 30);

        // encryption with public key of other user
        var encryptedForContact = this.ntruKex.Encrypt(this.contact.PublicNtruKey, this.OutGoingText);
        var tryteContact = new TryteString(encryptedForContact.ToTrytes() + ChiotaIdentifier.FirstBreak + signature + ChiotaIdentifier.SecondBreak + trytesDate + ChiotaIdentifier.End);

        // encryption with public key of user
        var encryptedForUser = this.ntruKex.Encrypt(this.user.NtruKeyPair.PublicKey, this.OutGoingText);
        var tryteUser = new TryteString(encryptedForUser.ToTrytes() + ChiotaIdentifier.FirstBreak + signature + ChiotaIdentifier.SecondBreak + trytesDate + ChiotaIdentifier.End);

        await this.SendParallelAsync(tryteContact, tryteUser);
      }

      this.IsBusy = false;
      this.OutGoingText = null;
    }

    private Task SendParallelAsync(TryteString tryteContact, TryteString tryteUser)
    {
      var firstTransaction = this.user.TangleMessenger.SendMessageAsync(tryteContact, this.contact.ChatAdress);
      var secondTransaction = this.user.TangleMessenger.SendMessageAsync(tryteUser, this.contact.ChatAdress);

      return Task.WhenAll(firstTransaction, secondTransaction);
    }

    private async void GetMessagesAsync(ICollection<MessageViewModel> messages)
    {
      while (this.PageIsShown)
      {
        await this.AddNewMessagesAsync(messages);
        await Task.Delay(9000);
      }
    }

    private async Task AddNewMessagesAsync(ICollection<MessageViewModel> messages)
    {
      var newMessages = await IotaHelper.GetNewMessages(this.user.NtruKeyPair, this.contact, this.user.TangleMessenger);
      if (newMessages.Count > 0)
      {
        foreach (var m in newMessages)
        {
          messages.Add(m);
        }

        this.ScrollToNewMessage();
      }
    }

    private void ScrollToNewMessage()
    {
      var lastMessage = this.messagesListView?.ItemsSource?.Cast<object>()?.LastOrDefault();

      if (lastMessage != null)
      {
        this.messagesListView.ScrollTo(lastMessage, ScrollToPosition.MakeVisible, true);
      }
    }
  }
}