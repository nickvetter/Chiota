﻿namespace Chiota.Models.SqLite
{
  using SQLite;

  public class SqLiteMessage
  {
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string TransactionHash { get; set; }

    [MaxLength(100)]
    public string ChatAddress { get; set; }

    public string MessageTryteString { get; set; }
  }
}
