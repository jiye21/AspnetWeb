using AspnetWeb.DataContext;
using AspnetWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;


namespace AspnetWeb.Controllers
{
	public class HomeController : Controller
    {
		private readonly IDistributedCache _redisCache;
        private readonly IAuthService _authService;
        private readonly AspnetNoteDbContext _dbContext;

        public HomeController(IDistributedCache redisCache, IAuthService authService,
            AspnetNoteDbContext dbContext)
		{
			_redisCache = redisCache;
            _authService = authService;
            _dbContext = dbContext;
		}

		public IActionResult Index()
        {
            return View();
        }

		[Route("/api/Home/MemberIndex")]
		public IActionResult MemberIndex()
        {
            // ViewBag�� ������ ��� ����� view�� �ٷ� ������ ���� ��밡��
			ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData

			return View();
		}


		// ���� �ڵ带 ���������� ��ȯ�Ͽ� �׼��� ��ū�� �޾ƿ���
		// �̴� ����ڰ� ���������� �����ǰ� ȸ�������� �Ϸ�Ǿ����� �ǹ�
		public async Task<IActionResult> GoogleUserEmailList(string code)
        {
            // ASP.NET Core������ �ڵ����� ���� ���ڿ� �Ű������� ��Ʈ�ѷ� �׼� �޼����� �Ű������� ���ε��Ѵ�.
            // ���� GoogleUserEmailList �޼����� code �Ű������� ���� �ڵ尡 ���޵ȴ�. 
            if(string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Login", "Account");
            }

			// GetGoogleUser: �̹� Google ȸ�������� ���� ������ ������. 
			// ���ԵǾ����� �ʴٸ� Google���� �޾ƿ� �����̸�,�̸��� ���� ������ oAuthUser�� ȸ�����Խ�Ű�� �� ���������� ������. 
			var googleUser = await _authService.GetGoogleUser(code);

            if (googleUser == null)   // ������ ���������� �޾ƿ��� ������ ��
            {
                return RedirectToAction("Login", "Account");
            }

            // �޾ƿ� Google OAuth���� ������ User���̺��� �˻��� �����̸��� �޾ƿ�
            var userInfo = _dbContext.Users.FirstOrDefault(u=>u.UID.Equals(googleUser.MUID));
            if (userInfo != null) 
            { 
                ViewData["Name"] = userInfo.UserName;
            }

            
            ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
            return View(googleUser);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
