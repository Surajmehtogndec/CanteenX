namespace CanteenX.Models.Home
{
    public class HomePageViewModel
    {
        // Pehli list Canteens ke liye
        public IEnumerable<CanteenCardDto> ActiveCanteens { get; set; } = new List<CanteenCardDto>();

        // Dusri list Menu Items ke liye
        public IEnumerable<MenuItemResponseDto> PopularItems { get; set; } = new List<MenuItemResponseDto>();
    }
}
