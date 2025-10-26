namespace ChatAppApi.Model
{
    public class Group
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;


    public ICollection<GroupMember> GroupMembers { get; set; }
        public ICollection<Message> Messages { get; set; }
    }

}
