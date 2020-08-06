
using Sitecore;
using Sitecore.Commerce.XA.Feature.Catalog.Cache;
using Sitecore.Commerce.XA.Feature.Catalog.MockData;
using Sitecore.Commerce.XA.Feature.Catalog.Models;
using Sitecore.Commerce.XA.Feature.Catalog.Models.JsonResults;
using Sitecore.Commerce.XA.Feature.Catalog.Repositories;
using Sitecore.Commerce.XA.Foundation.Catalog.Managers;
using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Search;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Entities;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using System.Collections.Generic;
using Diagnostics = Sitecore.Diagnostics;
using IBulkManager = Ajsuth.Foundation.Commerce.Engine.Connect.Managers.IBulkManager;
using Microsoft.Extensions.DependencyInjection;

namespace Ajsuth.Feature.Customers.Website.Repositories
{
    /// <summary>
    ///
    /// </summary>
    public class ProductListRepository : Sitecore.Commerce.XA.Feature.Catalog.Repositories.ProductListRepository, IProductListRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductListRepository" /> class.
        /// </summary>
        /// <param name="modelProvider">The model provider.</param>
        /// <param name="storefrontContext">The storefront context.</param>
        /// <param name="siteContext">The site context.</param>
        /// <param name="searchInformation">The search information.</param>
        /// <param name="searchManager">The search manager.</param>
        /// <param name="catalogManager">The catalog manager.</param>
        /// <param name="inventoryManager">The inventory manager.</param>
        /// <param name="catalogUrlManager">The catalog URL manager.</param>
        /// <param name="context">sitecore context</param>
        /// <param name="productListCacheProvider">The product list cache provider.</param>
        /// <param name="bulkManager">The bulk manager.</param>
        public ProductListRepository(
            [NotNull] IModelProvider modelProvider,
            [NotNull] IStorefrontContext storefrontContext,
            [NotNull] ISiteContext siteContext,
            [NotNull] ISearchInformation searchInformation,
            [NotNull] ISearchManager searchManager,
            [NotNull] ICatalogManager catalogManager,
            [NotNull] IInventoryManager inventoryManager,
            [NotNull] ICatalogUrlManager catalogUrlManager,
            [NotNull] IContext context,
            [NotNull] IProductListCacheProvider productListCacheProvider,
            [NotNull] IBulkManager bulkManager)
            : base(modelProvider,
                  storefrontContext,
                  siteContext,
                  searchInformation,
                  searchManager,
                  catalogManager,
                  inventoryManager,
                  catalogUrlManager,
                  context,
                  productListCacheProvider,
                  bulkManager)
        {
        }

        /// <summary>
        /// Adjusts the product prices and stock statuses.
        /// </summary>
        /// <param name="visitorContext">The visitor context.</param>
        /// <param name="searchResult">The search result.</param>
        /// <param name="currentCategory">The current category.</param>
        /// <param name="propertyBag">The property bag.</param>
        /// <returns>
        /// A list of product entities.
        /// </returns>
        protected override ICollection<ProductEntity> AdjustProductPriceAndStockStatus(
            IVisitorContext visitorContext,
            SearchResults searchResult,
            Item currentCategory,
            StringPropertyCollection propertyBag = null)
        {
            Diagnostics.Assert.ArgumentNotNull(currentCategory, nameof(currentCategory));
            Diagnostics.Assert.ArgumentNotNull(searchResult, nameof(searchResult));

            var storefront = StorefrontContext.CurrentStorefront;
            var productEntityList = new List<ProductEntity>();
            string cacheKey = "Category/" + currentCategory.Name;

            if (SiteContext.Items[cacheKey] != null)
            {
                return (List<ProductEntity>)SiteContext.Items[cacheKey];
            }

            if (searchResult.SearchResultItems != null && searchResult.SearchResultItems.Count > 0)
            {
                foreach (var productItem in searchResult.SearchResultItems)
                {
                    var productEntity = ModelProvider.GetModel<ProductEntity>();
                    productEntity.Initialize(storefront, productItem);
                    productEntity.CustomerAverageRating = CatalogManager.GetProductRating(productItem, propertyBag);
                    productEntityList.Add(productEntity);
                }

                var test = ProductListRepository.Current;

                ((Ajsuth.Foundation.Commerce.Engine.Connect.Managers.BulkManager)BulkManager).GetProductPricesAndStockInformation(storefront, visitorContext, productEntityList, false, propertyBag, null);
            }

            SiteContext.Items[cacheKey] = productEntityList;

            return productEntityList;
        }

        public static IVisitorContext Current
        {
            get
            {
                var siteContext = ServiceLocator.ServiceProvider.GetService<ISiteContext>();
                if (!siteContext.Items.Contains("StorefrontVisitorContext"))
                {
                    var visitorContext = ServiceLocator.ServiceProvider.GetService<IVisitorContext>();

                    siteContext.Items["StorefrontVisitorContext"] = visitorContext;
                }

                return siteContext.Items["StorefrontVisitorContext"] as IVisitorContext;
            }
        }
    }
}