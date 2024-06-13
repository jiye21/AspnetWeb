using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    public class AspnetUser
    {
        // UID, ID, PW, Salt
        /// <summary>
        /// 공통테이블 User의 UID와 동일한 값
        /// </summary>
        // PK는 속성 변경이 어려우니 신중히 생성하기
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // 자동 생성 해제
        public int UID { get; set; }

        /// <summary>
        /// 사용자 ID
        /// </summary>
        [Required(ErrorMessage = "사용자 ID를 입력하세요. ")]  // Not Null 설정
        public string UserId { get; set; }

        /// <summary>
        /// 사용자 Password
        /// </summary>
        [Required(ErrorMessage = "사용자 비밀번호를 입력하세요. ")]  // Not Null 설정
        public string UserPassword { get; set; }

        public string Salt { get; set; }
    }
}
