namespace EbayClone.Frontend.Services
{
    /// <summary>
    /// Singleton service lưu category data trong browser memory.
    /// - CategoryService (Scoped) không thể là Singleton vì inject HttpClient (Scoped).
    /// - Giải pháp: tách cache storage ra Singleton riêng, CategoryService vẫn Scoped.
    /// 
    /// Lifetime:
    ///   CategoryCacheService (Singleton) = tồn tại suốt vòng đời app
    ///   CategoryService (Scoped)         = inject CategoryCacheService, check cache trước
    /// </summary>
    public class CategoryCacheService
    {
        // Root categories (parentId = null)
        public List<CategoryTreeNodeDto>? RootCategories { get; set; }

        // Children cache keyed by parentId
        public Dictionary<Guid, List<CategoryTreeNodeDto>> ChildrenCache { get; } = new();

        // All categories (flat list, cho fallback search)
        public List<CategoryTreeNodeDto>? AllCategories { get; set; }

        // Clear cache (gọi khi admin thêm/sửa/xóa category)
        public void Invalidate()
        {
            RootCategories = null;
            AllCategories = null;
            ChildrenCache.Clear();
        }
    }
}
