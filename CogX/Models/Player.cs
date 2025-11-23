namespace CogX.Models
{
    public class Player
    {
        public Guid Id { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
