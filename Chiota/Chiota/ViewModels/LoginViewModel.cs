﻿namespace Chiota.ViewModels
{
  using System;
  using System.Threading.Tasks;
  using System.Windows.Input;

  using Chiota.Events;
  using Chiota.Models;
  using Chiota.Services;
  using Chiota.Services.DependencyInjection;
  using Chiota.Services.Iota;
  using Chiota.Services.Navigation;
  using Chiota.Services.Storage;
  using Chiota.Services.UserServices;
  using Chiota.Views;

  using Tangle.Net.Entity;
  using Tangle.Net.Utils;

  using Xamarin.Forms;

  public class LoginViewModel : BaseViewModel
  {
    public Action DisplayInvalidLoginPrompt;

    public Action DisplaySeedCopiedPrompt;

    private string randomSeed = Seed.Random().Value;

    private bool storeSeed;

    private User user;

    public LoginViewModel()
    {
      this.StoreSeed = true;
      this.UserFactory = DependencyResolver.Resolve<IUserFactory>();
    }

    /// <summary>
    /// Event raised as soon as a user logged in successfully.
    /// Outputs EventArgs of type <see cref="LoginEventArgs"/>
    /// </summary>
    public static event EventHandler LoginSuccessful;

    public bool StoreSeed
    {
      get => this.storeSeed;
      set
      {
        this.storeSeed = value;
        this.RaisePropertyChanged();
      }
    }

    public string RandomSeed
    {
      get => this.randomSeed ?? string.Empty;
      set
      {
        this.randomSeed = value;
        this.RaisePropertyChanged();
      }
    }

    public ICommand SubmitCommand => new Command(async () => { await this.Login(); });

    public ICommand CopySeedCommand => new Command(this.CopySeed);

    private IUserFactory UserFactory { get; }

    private void CopySeed()
    {
      this.DisplaySeedCopiedPrompt();
      DependencyResolver.Resolve<IClipboardService>().SendTextToClipboard(this.RandomSeed);
    }

    private async Task Login()
    {
      this.RandomSeed = this.RandomSeed.Trim();
      if (!InputValidator.IsTrytes(this.RandomSeed))
      {
        this.DisplayInvalidLoginPrompt();
      }
      else if (!this.IsBusy)
      {
        this.IsBusy = true;

        if (this.UserNotYetGenerated())
        {
          this.user = await this.UserFactory.CreateAsync(new Seed(this.RandomSeed), this.StoreSeed);
        }

        var publicKeyList = await IotaHelper.GetPublicKeysAndContactAddresses(this.user.TangleMessenger, this.user.PublicKeyAddress);

        // PublicKeyList should never be zero if this seed was used before (checks sqlite)
        if (publicKeyList.Count == 0)
        {
          this.user.ImageUrl = Application.Current.Properties[ChiotaConstants.SettingsImageKey + this.user.PublicKeyAddress] as string;
          this.user.Name = Application.Current.Properties[ChiotaConstants.SettingsNameKey + this.user.PublicKeyAddress] as string;
          await this.Navigation.PushModalAsync(new NavigationPage(new CheckSeedStoredPage(this.user)));
        }
        else
        {
          this.user = await new UserDataOnTangle(this.user).UniquePublicKey();
          new SecureStorage().StoreUser(this.user);

          if (this.user.NtruKeyPair != null)
          {
            LoginSuccessful?.Invoke(this, new LoginEventArgs { User = this.user });
            UserService.SetCurrentUser(this.user);
            Application.Current.MainPage = new NavigationPage(DependencyResolver.Resolve<INavigationService>().LoggedInEntryPoint);
            await this.Navigation.PopToRootAsync(true);
          }
          else
          {
            this.DisplayInvalidLoginPrompt();
          }
        }

        this.IsBusy = false;
      }
    }

    private bool UserNotYetGenerated()
    {
      return this.user == null || this.RandomSeed != this.user?.Seed.Value;
    }
  }
}