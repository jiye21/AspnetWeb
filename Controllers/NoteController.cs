using AspnetWeb.DataContext;
using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
			List<ShoppingList> shoppingListInfo = new List<ShoppingList>();
			long? userMUID = GetUserMUID();

			// HasValue: 값이 있는 경우- true, 값이 없는 경우(Null)- false
			if (userMUID.HasValue)
			{
				ViewData["ShopPage"] = string.Empty;
				ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData

				var shoppingListValue = _redisCache.GetString("shoppinglist_" + userMUID.ToString());
				if (!string.IsNullOrEmpty(shoppingListValue))
				{
					// 각 필드를 list로 변환
					// JSON 문자열을 JObject로 파싱
					JObject jsonData = JObject.Parse(shoppingListValue);

					// 각 필드를 리스트로 변환
					List<string> UIDList = jsonData["UID"].ToObject<List<string>>();
					List<string> MUIDList = jsonData["MUID"].ToObject<List<string>>();
					List<string> productList = jsonData["Product"].ToObject<List<string>>();
					List<string> priceList = jsonData["Price"].ToObject<List<string>>();
					List<string> countList = jsonData["Count"].ToObject<List<string>>();
					List<string> purchaseDateList = jsonData["PurchaseDate"].ToObject<List<string>>();

					// List<ShoppingList>로 변환
					for (int i = 0; i < UIDList.Count; i++)
					{
						ShoppingList s = new ShoppingList
						{
							UID = long.Parse(UIDList[i]),
							MUID = long.Parse(MUIDList[i]),
							Product = productList[i],
							Price = int.Parse(priceList[i]),
							Count = int.Parse(countList[i]),
							PurchaseDate = DateTimeOffset.Parse(purchaseDateList[i])
						};
						shoppingListInfo.Add(s);
					}

				}
				else
				{
					shoppingListInfo = await _dbContext.ShoppingList.
						Where(s => s.MUID.Equals(userMUID)).ToListAsync();

					List<string> UIDList = new List<string>();
					List<string> MUIDList = new List<string>();
					List<string> productList = new List<string>();
					List<string> priceList = new List<string>();
					List<string> countList = new List<string>();
					List<string> purchaseDateList = new List<string>();
					foreach (ShoppingList s in shoppingListInfo)
					{
						productList.Add(s.Product);
						priceList.Add(s.Price.ToString());
						countList.Add(s.Count.ToString());
						purchaseDateList.Add(s.PurchaseDate.ToString());
						UIDList.Add(s.UID.ToString());
						MUIDList.Add(s.MUID.ToString());
					}


					string shoppingKey = "shoppinglist_" + userMUID.ToString();

					JObject jsonData = new JObject(
						new JProperty("UID", JArray.FromObject(UIDList)),
						new JProperty("MUID", JArray.FromObject(MUIDList)),
						new JProperty("Product", JArray.FromObject(productList)),
						new JProperty("Price", JArray.FromObject(priceList)),
						new JProperty("Count", JArray.FromObject(countList)),
						new JProperty("PurchaseDate", JArray.FromObject(purchaseDateList))
					);
					string shoppingValue = JsonConvert.SerializeObject(jsonData);

					_redisCache.SetString(shoppingKey, shoppingValue);
				}
			}


			return View(shoppingListInfo);
		}

		/// <summary>
		/// 구매버튼 클릭 시 호출됨
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		[Route("/api/Note/ShoppingList")]
		public async Task<IActionResult> ShoppingList([FromBody] ShoppingListViewModel model)
		{
			if (ModelState.IsValid)
			{
				long? userMUID = GetUserMUID();

				if (userMUID != null)
				{
					// 새로 구입한 물건 추가
					var shoppingList = new ShoppingList
					{
						Product = model.Product,
						Price = model.Price,
						Count = model.Count,
						PurchaseDate = model.PurchaseDate,
						MUID = Convert.ToInt64(userMUID)
					};
					// DB에 저장
					await _dbContext.ShoppingList.AddAsync(shoppingList);
					_dbContext.SaveChanges();  // 기본 키 값이 설정됨

					// redis에 변동사항 저장
					SetShoppingListInRedis(Convert.ToInt64(userMUID));
				}
			}

			return Redirect("https://localhost:44396/Note/Shop");
		}

		/// <summary>
		/// redis에 쇼핑목록 저장
		/// </summary>
		/// <param name="userMUID"></param>
		public async void SetShoppingListInRedis(long userMUID)
		{
			List<ShoppingList> shoppingListInfo = await _dbContext.ShoppingList.
									Where(s => s.MUID.Equals(userMUID)).ToListAsync();
			List<string> UIDList = new List<string>();
			List<string> MUIDList = new List<string>();
			List<string> productList = new List<string>();
			List<string> priceList = new List<string>();
			List<string> countList = new List<string>();
			List<string> purchaseDateList = new List<string>();
			foreach (ShoppingList s in shoppingListInfo)
			{
				productList.Add(s.Product);
				priceList.Add(s.Price.ToString());
				countList.Add(s.Count.ToString());
				purchaseDateList.Add(s.PurchaseDate.ToString());
				UIDList.Add(s.UID.ToString());
				MUIDList.Add(s.MUID.ToString());
			}


			string shoppingKey = "shoppinglist_" + userMUID.ToString();

			JObject jsonData = new JObject(
				new JProperty("UID", JArray.FromObject(UIDList)),
				new JProperty("MUID", JArray.FromObject(MUIDList)),
				new JProperty("Product", JArray.FromObject(productList)),
				new JProperty("Price", JArray.FromObject(priceList)),
				new JProperty("Count", JArray.FromObject(countList)),
				new JProperty("PurchaseDate", JArray.FromObject(purchaseDateList))
			);
			string shoppingValue = JsonConvert.SerializeObject(jsonData);

			_redisCache.SetString(shoppingKey, shoppingValue);
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
