namespace ContentCartel.Models
{
    public class PortfolioItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CoverImageUrl { get; set; } = string.Empty;

        public List<PortfolioSection> Sections { get; set; } = new();
    }

    public class PortfolioSection
    {
        public string Heading { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<PortfolioImage> Images { get; set; } = new();
    }

    public class PortfolioImage
    {
        public string ImageUrl { get; set; } = string.Empty;

        public string AltText { get; set; } = string.Empty;
    }
}