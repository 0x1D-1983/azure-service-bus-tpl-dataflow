namespace TplAzureServiceBusWorker.Models
{
    public class Item
    {
        private string[] _colors = { "Red", "Green", "Blue", "Orange", "Yellow" };
        private double[] _prices = { 1.4, 2.3, 3.2, 4.1, 5.1 };
        private string[] _categories = { "Vegetables", "Beverage", "Meat", "Bread", "Other" };


        public string? Color { get; set; }
        public double? Price { get; set; }
        public string? Category { get; set; }

        public Item() { }

        public Item(int color, int price, int category)
        {
            SetColor(color);
            SetPrice(price);
            SetCategory(category);
        }

        public void SetColor(int color)
        {
            Color = _colors[color];
        }

        public void SetPrice(int price)
        {
            Price = _prices[price];
        }

        public void SetCategory(int ItmCat)
        {
            Category = _categories[ItmCat];
        }
    }
}

