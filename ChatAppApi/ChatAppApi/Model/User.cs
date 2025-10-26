namespace ChatAppApi.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }


        // Navigation
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<GroupMember> GroupMembers { get; set; }
    }

}
