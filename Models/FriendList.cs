using System.ComponentModel.DataAnnotations;

namespace AspnetWeb.Models
{
    public class FriendList
	{
		// UID(고유식별값), Friend 이름, Heart 개수, mUID(User테이블, 어떤유저의 친구목록인지 판별)
		[Key]
		public long UID { get; set; }

        public string FriendName { get; set; }

        public long FriendMUID { get; set; }

        public int HeartCount { get; set; }

		public long MUID { get; set; }
    }
}
