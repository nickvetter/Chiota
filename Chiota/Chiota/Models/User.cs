﻿namespace Chiota.Models
{
  using Chiota.Services.Iota;

  using Tangle.Net.Entity;

  using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;

  public class User
  {
    public string Name { get; set; }

    public string ImageUrl { get; set; }

    public string RequestAddress { get; set; }

    public string PublicKeyAddress { get; set; }

    public bool StoreSeed { get; set; }

    /// <summary>
    /// Gets or sets Chat Key Pair
    /// </summary>
    public IAsymmetricKeyPair NtruKeyPair { get; set; }

    /// <summary>
    /// Gets or sets Seed, never upload or store!
    /// </summary>
    public Seed Seed { get; set; }

    /// <summary>
    /// Gets or sets TangleMessenger, never upload or store!
    /// </summary>
    public TangleMessenger TangleMessenger { get; set; }
  }
}
