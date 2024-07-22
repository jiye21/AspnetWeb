using System.ComponentModel.DataAnnotations;

namespace AspnetWeb.ViewModel
{
    public class ShoppingListViewModel
	{
        public string Product { get; set; }

        public int Count { get; set; }

        public int Price { get; set; }

        public DateTimeOffset PurchaseDate { get; set; }

	}
}
