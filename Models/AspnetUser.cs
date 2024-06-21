using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    public class AspnetUser
    {
        // UID, ID, PW, Salt, muid
        [Key]
        public long UID { get; set; }

        [Required(ErrorMessage = "사용자 ID를 입력하세요. ")]  // Not Null 설정
        public string UserId { get; set; }

        [Required(ErrorMessage = "사용자 비밀번호를 입력하세요. ")]  // Not Null 설정
        public string UserPassword { get; set; }

        public string Salt { get; set; }

        public long MUID { get; set; }
    }
}
