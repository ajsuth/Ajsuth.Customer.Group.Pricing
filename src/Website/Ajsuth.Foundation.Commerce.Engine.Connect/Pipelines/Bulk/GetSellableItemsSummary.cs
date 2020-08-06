// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetSellableItemsSummary.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2018
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Commerce.Engine.Connect.Entities;
using Sitecore.Commerce.Engine.Connect.Extensions;
using Sitecore.Commerce.Engine.Connect.Pipelines.Bulk;
using Sitecore.Commerce.Engine.Connect.Services.Bulk;
using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Pipelines;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Commerce.ServiceProxy;
using Sitecore.Diagnostics;
using Diagnostics = Sitecore.Diagnostics;

namespace Ajsuth.Foundation.Commerce.Engine.Connect.Pipelines.Bulk
{
    /// <summary>
    /// Defines the get sellable items summary pipeline processor
    /// </summary>
    /// <seealso cref="BulkProcessor" />
    public class GetSellableItemsSummary : BulkProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSellableItemsSummary"/> class.
        /// </summary>
        /// <param name="entityFactory">The entity factory.</param>
        public GetSellableItemsSummary(IEntityFactory entityFactory)
        {
            Assert.ArgumentNotNull(entityFactory, "entityFactory");

            EntityFactory = entityFactory;
        }

        /// <summary>
        /// Gets or sets the entity factory.
        /// </summary>
        /// <value>
        /// The entity factory.
        /// </value>
        public IEntityFactory EntityFactory { get; set; }

        /// <summary>
        /// Process the Pipeline event
        /// </summary>
        /// <param name="args">The arguments.</param>
        public override void Process(ServicePipelineArgs args)
        {
            PipelineUtility.ValidateArguments(args, out GetSellableItemsSummaryRequest request, out GetSellableItemsSummaryResult result);

            try
            {
                Diagnostics.Assert.IsNotNull(request.PricingRequest, nameof(request.PricingRequest));
                Diagnostics.Assert.IsNotNull(request.PricingRequest.ShopName, nameof(request.PricingRequest.ShopName));
                Diagnostics.Assert.IsNotNull(request.PricingRequest.ProductIds, nameof(request.PricingRequest.ProductIds));
                Diagnostics.Assert.IsNotNullOrEmpty(request.PricingRequest.ProductCatalogName, nameof(request.PricingRequest.ProductCatalogName));
                Diagnostics.Assert.IsNotNull(request.StockInformationRequest, nameof(request.StockInformationRequest));
                Diagnostics.Assert.IsNotNullOrEmpty(request.StockInformationRequest.GetShopName(), nameof(request.StockInformationRequest.Shop.Name));
                Diagnostics.Assert.IsNotNull(request.StockInformationRequest.Products, nameof(request.StockInformationRequest.Products));
                Diagnostics.Assert.IsNotNull(request.StockInformationRequest.IncludeBundledItemsInventory, nameof(request.StockInformationRequest.IncludeBundledItemsInventory));
                Diagnostics.Assert.IsFalse(!request.PricingRequest.ShopName.Equals(request.StockInformationRequest.GetShopName(), StringComparison.OrdinalIgnoreCase), "Shop name does not match.");

                var pricingProductIds = request.PricingRequest.ProductIds.Select(x => string.Concat(request.PricingRequest.ProductCatalogName, PipelineUtility.SellableItemsIdIdDelimiter, x, PipelineUtility.SellableItemsIdIdDelimiter)).ToList();
                var inventoryProducts = request.StockInformationRequest.Products.Cast<CommerceInventoryProduct>();
                var inventoryProductIds = inventoryProducts.Select(x => string.Concat(x.CatalogName, PipelineUtility.SellableItemsIdIdDelimiter, x.ProductId, PipelineUtility.SellableItemsIdIdDelimiter, x.VariantId ?? string.Empty)).ToList();
                var itemIds = pricingProductIds.Union(inventoryProductIds).Distinct().ToList();

                if (!itemIds.Any())
                {
                    result.Success = true;
                    return;
                }

                var date = request.PricingRequest.DateTime == DateTime.MinValue ? null : (DateTime?)request.PricingRequest.DateTime;
                var container = GetContainer(request.PricingRequest.ShopName, request.PricingRequest.UserId, string.Empty, string.Empty, request.PricingRequest.CurrencyCode, date);
                var items = Proxy.Execute(container.GetSellableItemsSummary(itemIds, request.StockInformationRequest.IncludeBundledItemsInventory))?.ToList();
                if (items == null)
                {
                    result.Success = false;
                    return;
                }

                var pricingResults = new List<SellableItemPricing>();
                var inventoryResults = new List<ConnectItemAvailability>();
                items.ForEach(s =>
                {
                    pricingResults.AddRange(s.Summaries.OfType<SellableItemPricing>());
                    inventoryResults.AddRange(s.Summaries.OfType<ConnectItemAvailability>());
                });

                ProcessPricing(request.PricingRequest, result.PricingResult, pricingResults, EntityFactory);
                ProcessStockInformation(request.StockInformationRequest, result.StockInformationResult, inventoryResults);
            }
            catch (ArgumentException ex)
            {
                result.Success = false;
                result.SystemMessages.Add(PipelineUtility.CreateSystemMessage(ex));
            }
        }
    }
}
