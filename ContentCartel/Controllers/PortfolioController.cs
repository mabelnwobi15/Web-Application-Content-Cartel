using ContentCartel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class PortfolioController : Controller
    {
        private static readonly List<PortfolioItem> PortfolioItems =
            new List<PortfolioItem>
            {
                new PortfolioItem
                {
                    Id = 1,
                    Title = "Luxury Property Content",
                    Category = "Property & Architectural Content",
                    Description =
                        "A visual property showcase featuring architectural exteriors, " +
                        "styled interiors, outdoor spaces and detailed lifestyle imagery " +
                        "captured by Content Cartel.",

                    CoverImageUrl = "/images/portfolio/PZN00921.jpg",

                    Sections = new List<PortfolioSection>
                    {
                        new PortfolioSection
                        {
                            Heading = "Exterior Architecture",

                            Description =
                                "Clean architectural imagery highlighting the property's " +
                                "exterior design, structure and entrance spaces.",

                            Images = new List<PortfolioImage>
                            {
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00921.jpg",
                                    AltText = "Exterior view of modern property"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00928.jpg",
                                    AltText = "Modern property exterior and entrance"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00991.jpg",
                                    AltText = "Architectural exterior detail"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN01015.jpg",
                                    AltText = "Contemporary residential exterior"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN01019.jpg",
                                    AltText = "Exterior architectural view"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN01053.jpg",
                                    AltText = "Front exterior view of property"
                                }
                            }
                        },

                        new PortfolioSection
                        {
                            Heading = "Interior Spaces",

                            Description =
                                "Bright interior photography showcasing the property's " +
                                "living areas, layout and modern finishes.",

                            Images = new List<PortfolioImage>
                            {
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00940.jpg",
                                    AltText = "Modern interior living space"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00941.jpg",
                                    AltText = "Bright open-plan interior"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00945.jpg",
                                    AltText = "Styled modern interior"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00953.jpg",
                                    AltText = "Contemporary interior room"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00996.jpg",
                                    AltText = "Interior living area"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00997.jpg",
                                    AltText = "Modern residential interior"
                                }
                            }
                        },

                        new PortfolioSection
                        {
                            Heading = "Outdoor Living",

                            Description =
                                "Outdoor imagery capturing the pool, entertainment spaces " +
                                "and the connection between the property and its surroundings.",

                            Images = new List<PortfolioImage>
                            {
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00986.jpg",
                                    AltText = "Outdoor property area"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN01024.jpg",
                                    AltText = "Pool and outdoor living area"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN01027.jpg",
                                    AltText = "Outdoor entertainment space"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN01037.jpg",
                                    AltText = "Poolside architectural view"
                                }
                            }
                        },

                        new PortfolioSection
                        {
                            Heading = "Details & Styling",

                            Description =
                                "Detail-focused imagery highlighting décor, finishes and " +
                                "carefully styled elements throughout the property.",

                            Images = new List<PortfolioImage>
                            {
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00935.jpg",
                                    AltText = "Styled interior detail"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00947.jpg",
                                    AltText = "Interior design detail"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00966.jpg",
                                    AltText = "Property décor and finish detail"
                                },
                                new PortfolioImage
                                {
                                    ImageUrl = "/images/portfolio/PZN00974.jpg",
                                    AltText = "Decorative interior feature"
                                }
                            }
                        }
                    }
                }
            };

        public IActionResult Index()
        {
            return View(PortfolioItems);
        }

        public IActionResult Details(int id)
        {
            var item = PortfolioItems.FirstOrDefault(
                portfolioItem => portfolioItem.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }
    }
}