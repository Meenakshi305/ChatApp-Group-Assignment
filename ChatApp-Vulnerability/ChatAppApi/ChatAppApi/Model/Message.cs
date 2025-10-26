namespace ChatAppApi.Model
{
    public class Message
    {
        public int Id { get; set; }

        // Sender (required)
        public int SenderId { get; set; }
        public User Sender { get; set; }

        // Receiver (optional, for 1:1 chat)
        public int? ReceiverId { get; set; }
        public User Receiver { get; set; }

        // Group (optional, for group chat)
        public int? GroupId { get; set; }
        public Group Group { get; set; }

        public string MessageText { get; set; }
      

        // File properties
        public bool IsFile { get; set; } = false;
        public string? FileName { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
    }
}
