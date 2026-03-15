using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Services
{
    public interface ICategorySeeder
    {
        Task SeedCategoriesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// [A7] Seed dữ liệu Categories + CategoryItemSpecifics cho hệ thống.
    /// Chạy 1 lần khi khởi tạo DB. Nếu đã có data → bỏ qua.
    /// </summary>
    public class CategorySeeder : ICategorySeeder
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly EbayClone.Application.Interfaces.IUnitOfWork _unitOfWork;

        public CategorySeeder(ICategoryRepository categoryRepository, EbayClone.Application.Interfaces.IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task SeedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            // Check nếu đã có categories → không seed lại (idempotent)
            var existingCategories = await _categoryRepository.GetAllAsync(cancellationToken);
            if (existingCategories.Any())
                return;

            // ===== ROOT CATEGORIES (Level 0) =====
            var electronics = new Category { Name = "Electronics", Slug = "electronics", AttributeHints = "[\"Brand\",\"Model\",\"Storage\",\"Color\"]" };
            var fashion = new Category { Name = "Clothing & Accessories", Slug = "clothing-accessories", AttributeHints = "[\"Size\",\"Color\",\"Material\",\"Brand\"]" };
            var home = new Category { Name = "Home & Garden", Slug = "home-garden", AttributeHints = "[\"Material\",\"Color\",\"Brand\",\"Room\"]" };
            var sports = new Category { Name = "Sporting Goods", Slug = "sporting-goods", AttributeHints = "[\"Brand\",\"Size\",\"Color\",\"Sport\"]" };
            var collectibles = new Category { Name = "Collectibles & Art", Slug = "collectibles-art", AttributeHints = "[\"Era\",\"Origin\",\"Material\"]" };
            var motors = new Category { Name = "Motors", Slug = "motors", AttributeHints = "[\"Make\",\"Model\",\"Year\"]" };

            // ===== SUB-CATEGORIES (Level 1) =====
            // Electronics
            var phones = new Category { Name = "Cell Phones & Smartphones", Slug = "phones", ParentId = electronics.Id, AttributeHints = "[\"Brand\",\"Model\",\"Storage Capacity\",\"Color\",\"Network\"]" };
            var laptops = new Category { Name = "Laptops & Netbooks", Slug = "laptops", ParentId = electronics.Id, AttributeHints = "[\"Brand\",\"Processor\",\"RAM\",\"Storage\",\"Screen Size\"]" };
            var tablets = new Category { Name = "Tablets & eReaders", Slug = "tablets", ParentId = electronics.Id, AttributeHints = "[\"Brand\",\"Storage\",\"Screen Size\",\"Color\"]" };
            var cameras = new Category { Name = "Cameras & Photo", Slug = "cameras", ParentId = electronics.Id, AttributeHints = "[\"Brand\",\"Type\",\"Resolution\"]" };

            // Fashion
            var menClothing = new Category { Name = "Men's Clothing", Slug = "mens-clothing", ParentId = fashion.Id, AttributeHints = "[\"Size\",\"Color\",\"Material\",\"Brand\",\"Style\"]" };
            var womenClothing = new Category { Name = "Women's Clothing", Slug = "womens-clothing", ParentId = fashion.Id, AttributeHints = "[\"Size\",\"Color\",\"Material\",\"Brand\",\"Style\"]" };
            var shoes = new Category { Name = "Shoes", Slug = "shoes", ParentId = fashion.Id, AttributeHints = "[\"Size\",\"Color\",\"Brand\",\"Style\",\"Material\"]" };
            var watches = new Category { Name = "Watches", Slug = "watches", ParentId = fashion.Id, AttributeHints = "[\"Brand\",\"Movement\",\"Case Material\",\"Band Color\"]" };

            // Home & Garden
            var furniture = new Category { Name = "Furniture", Slug = "furniture", ParentId = home.Id, AttributeHints = "[\"Material\",\"Color\",\"Style\",\"Room\"]" };
            var kitchen = new Category { Name = "Kitchen & Dining", Slug = "kitchen-dining", ParentId = home.Id, AttributeHints = "[\"Brand\",\"Material\",\"Color\"]" };

            // Sports
            var fitness = new Category { Name = "Fitness Equipment", Slug = "fitness-equipment", ParentId = sports.Id, AttributeHints = "[\"Brand\",\"Type\",\"Weight\"]" };
            var cycling = new Category { Name = "Cycling", Slug = "cycling", ParentId = sports.Id, AttributeHints = "[\"Brand\",\"Frame Size\",\"Wheel Size\",\"Color\"]" };

            var allCategories = new List<Category>
            {
                electronics, fashion, home, sports, collectibles, motors,
                phones, laptops, tablets, cameras,
                menClothing, womenClothing, shoes, watches,
                furniture, kitchen,
                fitness, cycling
            };

            await _categoryRepository.AddRangeAsync(allCategories, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ===== ITEM SPECIFICS per Category =====
            var itemSpecifics = new List<CategoryItemSpecific>();

            // Phones - Required: Brand, Model, Storage | Recommended: Color, Network
            itemSpecifics.AddRange(CreateSpecifics(phones.Id, new[] {
                ("Brand", "REQUIRED", "[\"Apple\",\"Samsung\",\"Google\",\"OnePlus\",\"Xiaomi\",\"Huawei\",\"Sony\",\"LG\"]"),
                ("Model", "REQUIRED", (string?)null),
                ("Storage Capacity", "REQUIRED", "[\"32GB\",\"64GB\",\"128GB\",\"256GB\",\"512GB\",\"1TB\"]"),
                ("Color", "RECOMMENDED", "[\"Black\",\"White\",\"Blue\",\"Red\",\"Green\",\"Gold\",\"Silver\"]"),
                ("Network", "RECOMMENDED", "[\"Unlocked\",\"AT&T\",\"Verizon\",\"T-Mobile\",\"Sprint\"]"),
                ("RAM", "RECOMMENDED", "[\"4GB\",\"6GB\",\"8GB\",\"12GB\",\"16GB\"]"),
                ("Operating System", "RECOMMENDED", "[\"iOS\",\"Android\",\"HarmonyOS\"]")
            }));

            // Laptops - Required: Brand, Processor, RAM | Recommended: Storage, Screen Size
            itemSpecifics.AddRange(CreateSpecifics(laptops.Id, new[] {
                ("Brand", "REQUIRED", "[\"Apple\",\"Dell\",\"HP\",\"Lenovo\",\"ASUS\",\"Acer\",\"MSI\",\"Microsoft\"]"),
                ("Processor", "REQUIRED", "[\"Intel Core i3\",\"Intel Core i5\",\"Intel Core i7\",\"Intel Core i9\",\"AMD Ryzen 5\",\"AMD Ryzen 7\",\"Apple M1\",\"Apple M2\",\"Apple M3\"]"),
                ("RAM Size", "REQUIRED", "[\"4GB\",\"8GB\",\"16GB\",\"32GB\",\"64GB\"]"),
                ("SSD Capacity", "RECOMMENDED", "[\"128GB\",\"256GB\",\"512GB\",\"1TB\",\"2TB\"]"),
                ("Screen Size", "RECOMMENDED", "[\"11.6\\\"\",\"13.3\\\"\",\"14\\\"\",\"15.6\\\"\",\"16\\\"\",\"17.3\\\"\"]"),
                ("Operating System", "RECOMMENDED", "[\"Windows 11\",\"macOS\",\"Chrome OS\",\"Linux\"]"),
                ("GPU", "OPTIONAL", (string?)null)
            }));

            // Tablets
            itemSpecifics.AddRange(CreateSpecifics(tablets.Id, new[] {
                ("Brand", "REQUIRED", "[\"Apple\",\"Samsung\",\"Amazon\",\"Lenovo\",\"Microsoft\"]"),
                ("Storage Capacity", "REQUIRED", "[\"32GB\",\"64GB\",\"128GB\",\"256GB\",\"512GB\"]"),
                ("Screen Size", "RECOMMENDED", "[\"7\\\"\",\"8\\\"\",\"10.2\\\"\",\"10.9\\\"\",\"11\\\"\",\"12.9\\\"\"]"),
                ("Color", "RECOMMENDED", (string?)null)
            }));

            // Cameras
            itemSpecifics.AddRange(CreateSpecifics(cameras.Id, new[] {
                ("Brand", "REQUIRED", "[\"Canon\",\"Nikon\",\"Sony\",\"Fujifilm\",\"Panasonic\",\"Olympus\"]"),
                ("Type", "REQUIRED", "[\"DSLR\",\"Mirrorless\",\"Point & Shoot\",\"Film\",\"Action Camera\"]"),
                ("Resolution", "RECOMMENDED", (string?)null)
            }));

            // Men's Clothing
            itemSpecifics.AddRange(CreateSpecifics(menClothing.Id, new[] {
                ("Brand", "REQUIRED", (string?)null),
                ("Size", "REQUIRED", "[\"XS\",\"S\",\"M\",\"L\",\"XL\",\"XXL\",\"3XL\"]"),
                ("Color", "RECOMMENDED", (string?)null),
                ("Material", "RECOMMENDED", "[\"Cotton\",\"Polyester\",\"Linen\",\"Wool\",\"Silk\",\"Denim\"]"),
                ("Style", "RECOMMENDED", "[\"Casual\",\"Formal\",\"Athletic\",\"Streetwear\"]")
            }));

            // Women's Clothing
            itemSpecifics.AddRange(CreateSpecifics(womenClothing.Id, new[] {
                ("Brand", "REQUIRED", (string?)null),
                ("Size", "REQUIRED", "[\"XS\",\"S\",\"M\",\"L\",\"XL\",\"XXL\"]"),
                ("Color", "RECOMMENDED", (string?)null),
                ("Material", "RECOMMENDED", "[\"Cotton\",\"Polyester\",\"Linen\",\"Silk\",\"Chiffon\"]"),
                ("Style", "RECOMMENDED", "[\"Casual\",\"Formal\",\"Bohemian\",\"Vintage\"]")
            }));

            // Shoes
            itemSpecifics.AddRange(CreateSpecifics(shoes.Id, new[] {
                ("Brand", "REQUIRED", "[\"Nike\",\"Adidas\",\"New Balance\",\"Converse\",\"Vans\",\"Puma\",\"Reebok\"]"),
                ("US Shoe Size", "REQUIRED", (string?)null),
                ("Color", "RECOMMENDED", (string?)null),
                ("Style", "RECOMMENDED", "[\"Athletic\",\"Casual\",\"Dress\",\"Boots\",\"Sandals\"]"),
                ("Material", "RECOMMENDED", "[\"Leather\",\"Canvas\",\"Synthetic\",\"Suede\",\"Mesh\"]")
            }));

            // Watches
            itemSpecifics.AddRange(CreateSpecifics(watches.Id, new[] {
                ("Brand", "REQUIRED", "[\"Rolex\",\"Casio\",\"Seiko\",\"Citizen\",\"Omega\",\"TAG Heuer\",\"Timex\"]"),
                ("Movement", "REQUIRED", "[\"Automatic\",\"Quartz\",\"Mechanical\",\"Solar\"]"),
                ("Case Material", "RECOMMENDED", "[\"Stainless Steel\",\"Gold\",\"Titanium\",\"Ceramic\",\"Plastic\"]"),
                ("Band Color", "RECOMMENDED", (string?)null)
            }));

            // Furniture
            itemSpecifics.AddRange(CreateSpecifics(furniture.Id, new[] {
                ("Type", "REQUIRED", "[\"Sofa\",\"Table\",\"Chair\",\"Desk\",\"Bed\",\"Shelf\",\"Cabinet\"]"),
                ("Material", "REQUIRED", "[\"Wood\",\"Metal\",\"Glass\",\"Plastic\",\"Fabric\"]"),
                ("Color", "RECOMMENDED", (string?)null),
                ("Room", "RECOMMENDED", "[\"Living Room\",\"Bedroom\",\"Office\",\"Kitchen\",\"Bathroom\"]")
            }));

            // Kitchen
            itemSpecifics.AddRange(CreateSpecifics(kitchen.Id, new[] {
                ("Brand", "REQUIRED", (string?)null),
                ("Type", "REQUIRED", "[\"Cookware\",\"Bakeware\",\"Utensils\",\"Appliances\",\"Storage\"]"),
                ("Material", "RECOMMENDED", "[\"Stainless Steel\",\"Cast Iron\",\"Non-Stick\",\"Ceramic\",\"Glass\"]")
            }));

            // Fitness
            itemSpecifics.AddRange(CreateSpecifics(fitness.Id, new[] {
                ("Brand", "REQUIRED", (string?)null),
                ("Type", "REQUIRED", "[\"Treadmill\",\"Dumbbells\",\"Resistance Band\",\"Yoga Mat\",\"Bench\"]"),
                ("Weight", "RECOMMENDED", (string?)null)
            }));

            // Cycling
            itemSpecifics.AddRange(CreateSpecifics(cycling.Id, new[] {
                ("Brand", "REQUIRED", "[\"Trek\",\"Specialized\",\"Giant\",\"Cannondale\",\"Scott\"]"),
                ("Frame Size", "REQUIRED", (string?)null),
                ("Type", "RECOMMENDED", "[\"Road\",\"Mountain\",\"Hybrid\",\"BMX\",\"Electric\"]"),
                ("Wheel Size", "RECOMMENDED", "[\"20\\\"\",\"24\\\"\",\"26\\\"\",\"27.5\\\"\",\"29\\\"\",\"700c\"]")
            }));

            // Collectibles + Motors - chỉ RECOMMENDED, không REQUIRED
            itemSpecifics.AddRange(CreateSpecifics(collectibles.Id, new[] {
                ("Era/Year", "RECOMMENDED", (string?)null),
                ("Origin/Country", "RECOMMENDED", (string?)null),
                ("Material", "RECOMMENDED", (string?)null)
            }));

            itemSpecifics.AddRange(CreateSpecifics(motors.Id, new[] {
                ("Make", "REQUIRED", "[\"Toyota\",\"Honda\",\"Ford\",\"BMW\",\"Mercedes\",\"Audi\",\"Chevrolet\"]"),
                ("Model", "REQUIRED", (string?)null),
                ("Year", "REQUIRED", (string?)null),
                ("Mileage", "RECOMMENDED", (string?)null),
                ("Transmission", "RECOMMENDED", "[\"Automatic\",\"Manual\",\"CVT\"]")
            }));

            await _categoryRepository.AddItemSpecificsRangeAsync(itemSpecifics, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static List<CategoryItemSpecific> CreateSpecifics(Guid categoryId, (string Name, string Requirement, string? SuggestedValues)[] specs)
        {
            var list = new List<CategoryItemSpecific>();
            for (int i = 0; i < specs.Length; i++)
            {
                list.Add(new CategoryItemSpecific
                {
                    CategoryId = categoryId,
                    Name = specs[i].Name,
                    Requirement = specs[i].Requirement,
                    SuggestedValues = specs[i].SuggestedValues,
                    SortOrder = i
                });
            }
            return list;
        }
    }
}
