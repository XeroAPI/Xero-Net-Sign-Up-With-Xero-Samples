using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XeroNetSSUApp.Models
{
  public class User
  {
    public string Name { get; set; }

    public string Email { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    [Key]
    [Column("id")]
    public string XeroUserId { get; set; }

    public string SessionId { get; set; }

    public enum StatusEnum
    {
      LinkedToXero = 1,

      NotLinkedToXero = 2,
    }

    public StatusEnum Status { get; set; } 
  }
}
