using Sitecore;
using Sitecore.Analytics;
using Sitecore.Commerce.Engine.Connect;
using Sitecore.Commerce.Engine.Connect.Entities;
using Sitecore.Commerce.Engine.Connect.Services.Bulk;
using Sitecore.Commerce.Entities.Inventory;
using Sitecore.Commerce.Entities.Prices;
using Sitecore.Commerce.Services.Inventory;
using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Search;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Entities;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Commerce.XA.Foundation.Connect.Providers;
using Sitecore.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using CommerceInventoryProduct = Sitecore.Commerce.XA.Foundation.Common.Search.CommerceInventoryProduct;
using Microsoft.Extensions.DependencyInjection;

namespace Ajsuth.Foundation.Commerce.Engine.Connect.Managers
{
    public class BulkManager : Sitecore.Commerce.XA.Foundation.Connect.Managers.BulkManager, IBulkManager
    {
        private static readonly string[] DefaultPriceTypeIds = { PriceTypes.List, PriceTypes.Adjusted, PriceTypes.LowestPricedVariant, PriceTypes.LowestPricedVariantListPrice, PriceTypes.HighestPricedVariant };

        public BulkManager(
            [NotNull] IConnectServiceProvider connectServiceProvider,
            [NotNull] IStorefrontContext storefrontContext,
            [NotNull] ISiteContext siteContext)
            : base (connectServiceProvider, storefrontContext, siteContext)
        {
        }

        /// <summary>
        /// Gets the product prices and stock information.
        /// </summary>
        /// <param name="storefront">The storefront.</param>
        /// <param name="visitorContext">The visitor context.</param>
        /// <param name="productEntityList">The product entity list.</param>
        /// <param name="includeBundledItemsInventory">if set to <c>true</c> [include bundled items inventory].</param>
        /// <param name="propertyBag">The property bag.</param>
        /// <param name="priceTypeIds">The price type ids.</param>
        /// <returns>
        /// The manager response value.
        /// </returns>
        public virtual ManagerResponse<GetSellableItemsSummaryResult, bool> GetProductPricesAndStockInformation(
            CommerceStorefront storefront,
            IVisitorContext visitorContext,
            IEnumerable<ProductEntity>
            productEntityList,
            bool includeBundledItemsInventory,
            StringPropertyCollection propertyBag = null,
            params string[] priceTypeIds)
        {
            if (productEntityList == null || !productEntityList.Any())
            {
                return new ManagerResponse<GetSellableItemsSummaryResult, bool>(new GetSellableItemsSummaryResult(), true);
            }

            if (priceTypeIds == null)
            {
                priceTypeIds = DefaultPriceTypeIds;
            }

            var catalogName = productEntityList.Select(p => p.CatalogName).FirstOrDefault();
            var productIds = productEntityList.Select(p => p.ProductId).ToList();

            // TODO: Remove below code when CE Connect exposes the calalog name on items.
            if (string.IsNullOrWhiteSpace(catalogName))
            {
                catalogName = storefront.Catalog;
            }

            var test = VisitorContext.Current;

            if (!Tracker.IsActive && Tracker.Enabled && !Sitecore.Context.PageMode.IsExperienceEditor)
            {
                Tracker.StartTracking();
                visitorContext = ServiceLocator.ServiceProvider.GetService<IVisitorContext>();
            }

            // Setup the bulk price request.
            var bulkPriceRequest = new Sitecore.Commerce.Engine.Connect.Services.Prices.GetProductBulkPricesRequest(catalogName, productIds, priceTypeIds)
            {
                CurrencyCode = storefront.SelectedCurrency,
                DateTime = GetCurrentDate(),
                ShopName = storefront.ShopName,
                UserId = Context.User.IsAuthenticated ? visitorContext.CustomerId : string.Empty
            };

            // Setup the stock retrieval request
            var products = new List<CommerceInventoryProduct>();
            foreach (var viewModel in productEntityList)
            {
                products.Add(new CommerceInventoryProduct { ProductId = viewModel.ProductId, CatalogName = viewModel.CatalogName });
            }

            var csInventoryProductList = SearchHelper.ToCommerceEngineInventoryProducts(products);
            var inventoryRequest = new GetStockInformationRequest(storefront.ShopName, csInventoryProductList, StockDetailsLevel.All)
            {
                Location = string.Empty,
                IncludeBundledItemsInventory = includeBundledItemsInventory
            };

            var request = new GetSellableItemsSummaryRequest(bulkPriceRequest, inventoryRequest);
            var response = BulkServiceProvider.GetSellableItemsSummary(request);

            // Return inventory
            if (response.StockInformationResult != null && response.StockInformationResult.StockInformation != null)
            {
                var stockInfoList = response.StockInformationResult.StockInformation.ToList();

                foreach (var viewModel in productEntityList)
                {
                    StockInformation foundItem = null;

                    foundItem = stockInfoList.Find(p => string.Equals(p.Product.ProductId, viewModel.ProductId, StringComparison.Ordinal));
                    if (foundItem != null && foundItem.Status != null)
                    {
                        viewModel.StockStatus = foundItem.Status;
                        viewModel.StockStatusName = StorefrontContext.GetProductStockStatusName(foundItem.Status.Name);
                        viewModel.StockAvailabilityDate = foundItem.AvailabilityDate;
                    }
                }
            }

            // Return pricing
            var prices = response.PricingResult != null && response.PricingResult.Prices != null ? response.PricingResult.Prices : new Dictionary<string, Price>();

            foreach (var productEntity in productEntityList)
            {
                Price price;
                if (!prices.Any() || !prices.TryGetValue(productEntity.ProductId, out price))
                {
                    continue;
                }

                var extendedPrice = (CommercePrice)price;

                productEntity.ListPrice = extendedPrice.Amount;
                productEntity.AdjustedPrice = extendedPrice.ListPrice;
                productEntity.LowestPricedVariantAdjustedPrice = extendedPrice.LowestPricedVariant;
                productEntity.LowestPricedVariantListPrice = extendedPrice.LowestPricedVariantListPrice;
                productEntity.HighestPricedVariantAdjustedPrice = extendedPrice.HighestPricedVariant;
            }

            return new ManagerResponse<GetSellableItemsSummaryResult, bool>(response, response.Success);
        }

    }
}