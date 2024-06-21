using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    // UID(PK), NickName
    public class User
    {
        /// <summary>
        /// 사용자 번호
        /// </summary>
        [Key]   // PK 설정
		public long UID { get; set; }

        /// <summary>
        /// 사용자 이름
        /// </summary>
        [Required(ErrorMessage ="사용자 이름을 입력하세요. ")]  // Not Null 설정
        public string UserName { get; set; }

        public int LoginType { get; set; }
    }
}
