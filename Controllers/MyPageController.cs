﻿using AspnetWeb.DataContext;
using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace AspnetWeb.Controllers
{
	public class MyPageController : Controller
	{
		private readonly AspnetNoteDbContext _dbContext;
		private readonly IDistributedCache _redisCache;
		private readonly IConfiguration _configuration;
		IDatabase redisDb;
		private readonly IAuthService _authService;


		ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");

		public MyPageController(AspnetNoteDbContext dbContext, IDistributedCache redisCache,
			IConfiguration configuration, IAuthService authService)
		{
			_dbContext = dbContext;
			_redisCache = redisCache;
			_configuration = configuration;
			redisDb = redis.GetDatabase();
			_authService = authService;
		}

		// 나중에 redis 키 값을 변경하게 되는 경우에 필요하다
		public string getKey(long uid) {
			return "shoppinglist_" + uid.ToString();
		}

		
		public async Task<IActionResult> Shop()
		{
			List<ShoppingList> shoppingListInfo = new List<ShoppingList>();
			long userMUID = GetUserMUID();

			if (userMUID != 0)
			{

														// session 업데이트
														// getSession으로 따로 함수로 빼기
														// 로그인 안해도 되는 페이지는 checkSession같은 함수를 따로 두기
				string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
				if (!string.IsNullOrEmpty(sessionKey))
				{
					var sessionValue = _redisCache.Get(sessionKey);
					_authService.UpdateSessionAndCookie(sessionKey, sessionValue);

					ViewData["SESSION_KEY"] = string.Empty; // 내비게이션 바 변경을 위한 ViewData
				}

				string shoppingListValue = _redisCache.GetString(getKey(userMUID));

				// redis에 shoppingList 캐싱 데이터가 존재하는경우
				// 저장된 json 데이터를 파싱해 List<ShoppingList> 타입으로 변환

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
					// shoppingList를 받아와 json 데이터로 변환 후 redis에 set
					shoppingListInfo = await SetShoppingListInRedis(Convert.ToInt64(userMUID));
				}
			}

			return View(shoppingListInfo);
		}

		/// <summary>
		/// 구매버튼 클릭 시 호출됨, [/api] 미들웨어에서 세션 업데이트중
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		[Route("/api/MyPage/ShoppingList")]
		public async Task<IActionResult> ShoppingList([FromBody] List<ShoppingListViewModel> model)
		{
			// 어디에서 오류났는지 제대로 피드백을 주기
			if (ModelState.IsValid)
			{
				long? userMUID = GetUserMUID();

				if (userMUID.HasValue)
				{
					// 새로 구입한 물건 저장
					/*
					var shoppingList = new ShoppingList
					{
						Product = model.Product,
						Price = model.Price,
						Count = model.Count,
						PurchaseDate = model.PurchaseDate,
						MUID = Convert.ToInt64(userMUID)
					};
					*/
					foreach(var s in model)
					{
						var shoppingList = new ShoppingList
						{
							Product = s.Product,
							Price = s.Price,
							Count = s.Count,
							PurchaseDate = s.PurchaseDate,
							MUID = Convert.ToInt64(userMUID)
						};

						// DB에 저장
						await _dbContext.ShoppingList.AddAsync(shoppingList);  // 쿼리를 안쓴다 -> DAO에 파라미터를 DTO를 넣음
						_dbContext.SaveChanges();  // 기본 키 값이 설정됨
					}

					

					// redis에 변동사항 저장
					await SetShoppingListInRedis(Convert.ToInt64(userMUID));


					return Ok();
				}
			}

			return Unauthorized();
		}

		/// <summary>
		/// redis에 쇼핑목록 저장
		/// </summary>
		/// <param name="userMUID"></param>
		public async Task<List<ShoppingList>> SetShoppingListInRedis(long userMUID)
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

			return shoppingListInfo;
		}

		/// <summary>
		/// 세션이나 JWT의 claim부분에서 유저의 MUID를 검색
		/// </summary>
		/// <returns></returns>
		public long GetUserMUID()
		{
			long userMUID = 0;

			// 세션에서 유저의 MUID 가져옴
			string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
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
			if (this.ValidateToken(token) == true)
			{
				// 만료된 JWT에서 유저정보를 읽지 않기 위해 다시 JWT 검증
				var tokenHandler = new JwtSecurityTokenHandler();
				var accessToken = (JwtSecurityToken)tokenHandler.ReadToken(token);

				userMUID = Convert.ToInt64(accessToken.Claims.
					FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);				
			}

			return userMUID;
		}

		/// <summary>
		/// JWT 검증 함수
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		private bool ValidateToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
				return false;

			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var authSigningKey = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]);
				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(authSigningKey),
					ValidateIssuer = false,
					ValidateAudience = false,
					// set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
					ClockSkew = TimeSpan.Zero
				}, out SecurityToken validatedToken);

				// 만약 위 tokenHandler.ValidateToken에서 예외가 발생하지 않았다면 true 리턴
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		/// <summary>
		/// 유저의 친구 목록과 좋아요 수를 받아와 좋아요 랭킹 순으로 정렬
		/// </summary>
		/// <returns></returns>
		[Route("/api/MyPage/Friend")]
		public async Task<IActionResult> Friend()
		{
			List<FriendListViewModel> friendListInfo = new List<FriendListViewModel>();
			long userMUID = GetUserMUID();


			if (userMUID != 0)
			{
				ViewData["SESSION_KEY"] = string.Empty; // 내비게이션 바 변경을 위한 ViewData

				var friendListValue = _redisCache.GetString("friendlist_" + userMUID.ToString());

				// redis에 friendList 캐싱 데이터가 존재하는경우

				// byte로 저장하기!!!!!
				// byte로 redis에 set이 안된다면, 클래스를 통으로 json으로 바꾸기  string jsonString = JsonSerializer.Serialize(myObject);
				// 

				// redis에 get, set을 하는 함수를 따로 빼서 구현한다.
				// 이때 get을 할 땐 어떤 자료형으로 return 할 지 모르니 제일 부모 클래스인 System.Object를 리턴하도록 함수를 구현한다. 
				// 나중에 함수를 리턴받아 사용할 때는 MyClass a = (MyClass)GetRedisData(string key, <T>); 이렇게 사용
				// MyClass a = (MyClass)GetRedisData(string key, <T>);   return (<T>)getredis(key); 이런식으로 템플릿 사용도 가능! 더 고급스러움
				if (!string.IsNullOrEmpty(friendListValue))
				{
					JObject jsonData = JObject.Parse(friendListValue);

					List<string> friendNameList = jsonData["FriendName"].ToObject<List<string>>();
					List<string> friendMUIDList = jsonData["FriendMUID"].ToObject<List<string>>();
					List<string> heartCountList = jsonData["HeartCount"].ToObject<List<string>>();

					for (int i = 0; i < friendNameList.Count; i++)
					{
						FriendListViewModel f = new FriendListViewModel
						{
							FriendName = friendNameList[i],
							FriendMUID = Convert.ToInt64(friendMUIDList[i]),
							HeartCount = Convert.ToInt32(heartCountList[i]),
							Rank = i + 1
						};
						friendListInfo.Add(f);
					}
				}
				else
				{
					// mssql에서 friendList를 받아와 redis에 set한 후 friendListInfo에도 저장
					friendListInfo = await SetFriendListInRedis(Convert.ToInt64(userMUID));
				}
			}


			return View(friendListInfo);
		}

		/// <summary>
		/// redis에 FriendList를 좋아요 랭킹순으로 저장
		/// </summary>
		/// <param name="userMUID"></param>
		/// <returns></returns>
		///
		public async Task<List<FriendListViewModel>> SetFriendListInRedis(long userMUID)
		{

			List<FriendList> friendList = await _dbContext.FriendList.
									Where(f => f.MUID.Equals(userMUID)).ToListAsync();

			// friend_hearts 랭킹 초기화
			string friend_hearts = "friend_hearts_" + userMUID.ToString();
			redisDb.KeyDelete(friend_hearts);
			foreach (FriendList f in friendList)
			{
				redisDb.SortedSetAdd(friend_hearts, f.FriendName + "_" + f.FriendMUID, f.HeartCount);
			}

			// 내림차순으로 좋아요 랭킹 정렬
			SortedSetEntry[] heartRank = redisDb.SortedSetRangeByRankWithScores(friend_hearts, 0, -1, Order.Descending);

			List<FriendListViewModel> friendListViewModelList = new List<FriendListViewModel>(); // return용 데이터
			List<string> friendNameList = new List<string>(); // json.set용 데이터
			List<string> heartCountList = new List<string>(); // json.set용 데이터
			List<string> friendMUIDList = new List<string>(); // json용 데이터
			for (int i = 0; i < heartRank.Length; i++)
			{
				var friendName = heartRank[i].ToString().Split("_").First();
				var friendMUID = heartRank[i].ToString().Split('_').Last().Split(":").First();
				var heartCount = heartRank[i].ToString().Split(": ").Last();

				friendNameList.Add(friendName);
				friendMUIDList.Add(friendMUID);
				heartCountList.Add(heartCount);

				FriendListViewModel f = new FriendListViewModel
				{
					FriendName = friendName,
					FriendMUID = Convert.ToInt64(friendMUID),
					HeartCount = Convert.ToInt32(heartCount),
					Rank = i + 1
				};
				friendListViewModelList.Add(f);
			}


			string friendKey = "friendlist_" + userMUID.ToString();

			JObject jsonData = new JObject(
				new JProperty("FriendName", JArray.FromObject(friendNameList)),
				new JProperty("FriendMUID", JArray.FromObject(friendMUIDList)),
				new JProperty("HeartCount", JArray.FromObject(heartCountList))
			);
			string friendValue = JsonConvert.SerializeObject(jsonData);

			//redisDb.JSON().Set(friendKey, "$", friendValue);
			_redisCache.SetString(friendKey, friendValue);

			return friendListViewModelList;
		}

		/// <summary>
		/// 좋아요 수 변동시 DB에 반영하고 redis에 랭킹 다시 set
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		[Route("/api/MyPage/UpdateHeartCount")]
		public async Task<IActionResult> UpdateHeartCount([FromBody] FriendListViewModel model)
		{
			if (ModelState.IsValid)
			{
				long? userMUID = GetUserMUID();

				if (userMUID.HasValue)
				{
					var friend = _dbContext.FriendList.FirstOrDefault(f => f.FriendMUID.Equals(model.FriendMUID)
						&& f.MUID.Equals(userMUID));

					// 좋아요 수 업데이트
					friend.HeartCount = model.HeartCount;

					_dbContext.FriendList.Update(friend);
					_dbContext.SaveChanges();

					await SetFriendListInRedis(Convert.ToInt64(userMUID));

					return Ok();
				}
			}

			return BadRequest(ModelState);
		}

		/// <summary>
		/// 이름으로 유저 검색, 동명이인인 경우 대비해 muid까지 같이 return
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[HttpGet("/api/MyPage/SearchFriendWithName/{name}")]
		public IActionResult SearchFriendWithName(string name)
		{
			List<User> friend = _dbContext.Users.Where(f => f.UserName.Equals(name)).ToList();

			List<string> friendNameList = new List<string>();
			List<string> friendMUIDList = new List<string>();

			foreach (var user in friend)
			{
				friendNameList.Add(user.UserName);
				friendMUIDList.Add(user.UID.ToString());
			}


			IActionResult response = Ok(new
			{
				friendName = friendNameList,
				friendMUID = friendMUIDList
			});
			return response;
		}


		[HttpGet("/api/MyPage/AddFriend/{muid}")]
		public async Task<IActionResult> AddFriend(string muid)
		{
			long? userMUID = GetUserMUID();

			if (userMUID.HasValue)
			{
				// 추가할 유저 검색
				var friend = _dbContext.Users.FirstOrDefault(f => f.UID.Equals(Convert.ToInt64(muid)));

				// 친구 추가
				var friendList = new FriendList
				{
					FriendName = friend.UserName,
					HeartCount = 0,
					FriendMUID = Convert.ToInt64(muid),
					MUID = Convert.ToInt64(userMUID)
				};
				// DB에 저장
				await _dbContext.FriendList.AddAsync(friendList);
				_dbContext.SaveChanges();  // 기본 키 값이 설정됨

				// redis에 변동사항 저장
				await SetFriendListInRedis(Convert.ToInt64(userMUID));
				return Ok();
			}

			return Unauthorized();
		}

		[HttpGet("/api/MyPage/DeleteFriend/{friendMUID}")]
		public async Task<IActionResult> DeleteFriend(string friendMUID)
		{
			long? userMUID = GetUserMUID();

			if (userMUID.HasValue)
			{
				var deleteFriend = _dbContext.FriendList.FirstOrDefault(f => f.MUID.Equals(userMUID)
					&& f.FriendMUID.Equals(Convert.ToInt64(friendMUID)));
				// 친구 삭제
				_dbContext.FriendList.Remove(deleteFriend);
				_dbContext.SaveChanges();

				// redis에 변동사항 저장
				await SetFriendListInRedis(Convert.ToInt64(userMUID));
				return Ok();
			}

			return Unauthorized();
		}

	}
}
