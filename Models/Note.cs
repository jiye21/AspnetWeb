using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    // 게시물 번호(PK), 게시물 제목, 게시물 내용, 작성자 번호(UID, User 테이블 외래키)
	public class Note
	{
		[Key]
        public int NoteNo { get; set; }

        public string NoteTitle { get; set; }

        public string NoteContents { get; set; }

        public string UserName { get; set; }

        public long UID { get; set; }

        /*
        // lazy loading을 위해 virtual 키워드 권장됨
        // 게시판 작성자를 UID가 아닌 유저 닉네임으로 표시해야 하므로 join이 필요.
        [ForeignKey("UID")]
        public virtual User User { get; set; }
        */
    }
}
