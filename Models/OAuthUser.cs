using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    public class OAuthUser
    {
        // UID(PK, 공통), Guid, Email
        /// <summary>
        /// UID: 공통테이블 User의 UID와 동일한 값
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // 자동 생성 해제
        public int UID { get; set; }

        public string GoogleUID { get; set; }

        public string GoogleEmail { get; set; }

    }
}
