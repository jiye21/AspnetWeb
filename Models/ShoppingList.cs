using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetWeb.Models
{
    public class ShoppingList
	{
		// UID(고유식별값), product, price, count, PurchaseDate, mUID(User테이블)
		[Key]
		public long UID { get; set; }

        public string Product { get; set; }

        public int Price { get; set; }

        public int Count { get; set; }

        public DateTimeOffset PurchaseDate { get; set; }

        public long MUID { get; set; }
    }
}
