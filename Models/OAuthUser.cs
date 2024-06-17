using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    public class OAuthUser
    {
		// UID(고유식별값), Guid, Email, mUID(User테이블)
		[Key]
		public int UID { get; set; }

        public string GoogleUID { get; set; }

        public string GoogleEmail { get; set; }

        public int MUID { get; set; }
    }
}
