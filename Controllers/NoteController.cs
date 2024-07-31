using Microsoft.AspNetCore.Mvc;


namespace AspnetWeb.Controllers
{
	public class NoteController : Controller
	{
		

		/// <summary>
		/// 게시판 리스트 출력 페이지
		/// </summary>
		/// <returns></returns>
		public IActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// 게시물 추가
		/// </summary>
		/// <returns></returns>
		public IActionResult Add()
		{
			return View();
		}

		/// <summary>
		/// 게시물 수정
		/// </summary>
		/// <returns></returns>
		public IActionResult Edit()
		{
			return View();
		}

		/// <summary>
		/// 게시물 삭제
		/// </summary>
		/// <returns></returns>
		public IActionResult Delete()
		{
			return View();
		}


	}
}
