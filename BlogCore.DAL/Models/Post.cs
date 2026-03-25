namespace BlogCore.DAL.Models;

using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;


    // Post ma wiele Komentarzy
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
