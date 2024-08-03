using AspnetWeb.DataContext;
using AspnetWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace AspnetWeb.Controllers
{
	public class NoteController : Controller
	{
		private readonly AspnetNoteDbContext _dbContext;
		private readonly IDistributedCache _redisCache;

		public NoteController(AspnetNoteDbContext dbContext, IDistributedCache redisCache)
		{
			_dbContext = dbContext;
			_redisCache = redisCache;
		}

		/// <summary>
		/// 게시판 리스트 출력 페이지
		/// </summary>
		/// <returns></returns>
		public IActionResult Index()
		{
			var list = _dbContext.Notes.ToList();

			return View(list);
		}

		/// <summary>
		/// 게시판 상세페이지
		/// </summary>
		/// <param name="noteNo"></param>
		/// <returns></returns>
		public IActionResult Detail(int noteNo)
		{
			var note = _dbContext.Notes.FirstOrDefault(n => n.NoteNo.Equals(noteNo));

			return View(note);
		}

		/// <summary>
		/// 게시물 추가
		/// </summary>
		/// <returns></returns>
		[Route("/api/Note/Add")]
		public IActionResult Add()
		{
			return View();
		}

		[HttpPost]
		[Route("/api/Note/Add")]
		public IActionResult Add(Note model)
		{
			long? userUID = GetUserMUID();
			if(userUID.HasValue)
			{
				model.UID = userUID.Value;
				var user = _dbContext.Users.FirstOrDefault(u => u.UID.Equals(model.UID));
				model.UserName = user.UserName;
			}

			if(ModelState.IsValid)
			{
				_dbContext.Notes.Add(model);

				if(_dbContext.SaveChanges() > 0) // SaveChanges를 수행하면 결과로 성공한 갯수가 반환됨. 여기서는 하나만 저장하므로 1이 반환되어야 함
				{
					return Redirect("Index");
				}

				// 게시물 추가 실패
				ModelState.AddModelError(string.Empty, "게시물을 저장할 수 없습니다. ");
			}
			return View(model);
		}

		/// <summary>
		/// 게시물 수정
		/// </summary>
		/// <returns></returns>
		[Route("/api/Note/Edit")]
		public IActionResult Edit()
		{


			return View();
		}

		/// <summary>
		/// 게시물 삭제
		/// </summary>
		/// <returns></returns>
		[Route("/api/Note/Delete")]
		public IActionResult Delete()
		{


			return View();
		}

		public long? GetUserMUID()
		{
			// 세션에서 유저의 MUID 가져옴
			string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
			long? userMUID = null;

			if (!string.IsNullOrEmpty(sessionKey))
			{
				var sessionValue = _redisCache.Get(sessionKey);
				if (sessionValue != null)
				{
					userMUID = BitConverter.ToInt64(sessionValue);
					return userMUID;
				}
			}

			// 세션로그인이 아닐경우 JWT의 Claim에서 가져옴
			string token = HttpContext.Request.Cookies["JWT"];
			if (!string.IsNullOrEmpty(token))
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var accessToken = (JwtSecurityToken)tokenHandler.ReadToken(token);

				userMUID = Convert.ToInt64(accessToken.Claims.
					FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
			}


			return userMUID;
		}
	}
}
