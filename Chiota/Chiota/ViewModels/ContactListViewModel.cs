﻿namespace Chiota.ViewModels
{
  using System.Threading.Tasks;
  using System.Windows.Input;

  using Chiota.IOTAServices;
  using Chiota.Models;
  using Chiota.Services;

  using Tangle.Net.Entity;
  using Tangle.Net.Utils;

  using Xamarin.Forms;

  public class ContactListViewModel : Contact
  {
    private readonly User user;

    private readonly ViewCellObject viewCellObject;

    private bool isClicked;

    public ContactListViewModel(User user, ViewCellObject viewCellObject)
    {
      this.user = user;
      this.viewCellObject = viewCellObject;
      this.AcceptCommand = new Command(this.OnAccept);
      this.DeclineCommand = new Command(this.OnDecline);
    }

    public ICommand AcceptCommand { get; protected set; }

    public ICommand DeclineCommand { get; protected set; }

    private async void OnDecline()
    {
      if (!this.isClicked)
      {
        this.isClicked = true;

        var encryptedDecline = new NtruKex().Encrypt(this.user.NtruContactPair.PublicKey, this.ChatAdress + ChiotaIdentifier.Rejected);
        var tryteString = new TryteString(encryptedDecline.ToTrytes() + ChiotaIdentifier.End);

        await this.user.TangleMessenger.SendMessageAsync(tryteString, this.user.ApprovedAddress);
        this.viewCellObject.RefreshContacts = true;
        this.isClicked = false;
      }
    }

    private async void OnAccept()
    {
      if (!this.isClicked)
      {
        this.isClicked = true;

        await this.SendParallelAcceptAsync();

        this.viewCellObject.RefreshContacts = true;
        this.isClicked = false;
      }
    }

    // parallelize = only await for second PoW, when remote PoW 
    private Task SendParallelAcceptAsync()
    {
      var encryptedAccept = new NtruKex().Encrypt(this.user.NtruContactPair.PublicKey, this.ChatAdress + ChiotaIdentifier.Accepted);
      var tryteString = new TryteString(encryptedAccept.ToTrytes() + ChiotaIdentifier.End);

      // store as approved on own adress
      var firstTransaction = this.user.TangleMessenger.SendMessageAsync(tryteString, this.user.ApprovedAddress);

      var contact = new Contact
                      {
                        Name = this.user.Name,
                        ImageUrl = this.user.ImageUrl,
                        ChatAdress = this.ChatAdress,
                        ContactAdress = this.user.ApprovedAddress,
                        PublicKeyAdress = this.user.PublicKeyAddress,
                        Rejected = false,
                        Request = false
                      };

      // send data to request address, other user needs to automaticly add it to his own approved contact address
      var secondTransaction = this.user.TangleMessenger.SendMessageAsync(IotaHelper.ObjectToTryteString(contact), this.ContactAdress);
      return Task.WhenAll(firstTransaction, secondTransaction);
    }
  }
}
