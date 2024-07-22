using AspnetWeb.DataContext;
using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AspnetWeb.Controllers
{
	public class NoteController : Controller
	{
		private readonly AspnetNoteDbContext _dbContext;
		private readonly IDistributedCache _redisCache;
		private readonly IConfiguration _configuration;

		public NoteController(AspnetNoteDbContext dbContext, IDistributedCache redisCache, IConfiguration configuration)
		{
			_dbContext = dbContext;
			_redisCache = redisCache;
			_configuration = configuration;
		}


		public async Task<IActionResult> Shop()
		{
			List<ShoppingList> shoppingListInfo = null;
			long? userMUID = GetUserMUID();

			// HasValue: 값이 있는 경우- true, 값이 없는 경우(Null)- false
			if (userMUID.HasValue)
			{
				ViewData["ShopPage"] = string.Empty;
				ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData

				shoppingListInfo = await _dbContext.ShoppingList.
					Where(s => s.MUID.Equals(userMUID)).ToListAsync();
			}


			return View(shoppingListInfo);
		}

		[HttpPost]
		[Route("/api/Note/ShoppingList")]
		public async Task<IActionResult> ShoppingList([FromBody] ShoppingListViewModel model)
		{
			if (ModelState.IsValid)
			{
				long? userMUID = GetUserMUID();

				if (userMUID != null)
				{
					var shoppingList = new ShoppingList
					{
						Product = model.Product,
						Price = model.Price,
						Count = model.Count,
						PurchaseDate = model.PurchaseDate,
						MUID = Convert.ToInt64(userMUID)
					};
					await _dbContext.ShoppingList.AddAsync(shoppingList);
					_dbContext.SaveChanges();  // 기본 키 값이 설정됨
				}
			}

			return Redirect("https://localhost:44396/Note/Shop");
		}

		/// <summary>
		/// 세션이나 JWT의 claim부분에서 유저의 MUID를 검색
		/// </summary>
		/// <returns></returns>
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
			string token = HttpContext.Request.Cookies["AccessToken"];
			var tokenHandler = new JwtSecurityTokenHandler();
			var accessToken = (JwtSecurityToken)tokenHandler.ReadToken(token);

			userMUID = Convert.ToInt64(accessToken.Claims.
				FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);			

			return userMUID;
		}

		public IActionResult FriendList()
		{
			return View();
		}

		public IActionResult NotePad()
		{
			return View();
		}
	}
}
