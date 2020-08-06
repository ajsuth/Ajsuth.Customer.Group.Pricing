using Sitecore.Commerce.Engine.Connect.Services.Bulk;
using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Connect.Entities;
using System.Collections.Generic;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Commerce.XA.Foundation.Connect;

namespace Ajsuth.Foundation.Commerce.Engine.Connect.Managers
{
    public interface IBulkManager : Sitecore.Commerce.XA.Foundation.Connect.Managers.IBulkManager
    {
        /// <summary>
        /// Gets the product prices and stock information.
        /// </summary>
        /// <param name="storefront">The storefront.</param>
        /// <param name="visitorContext">The visitor context.</param>
        /// <param name="productEntityList">The product entity list.</param>
        /// <param name="includeBundledItemsInventory">if set to <c>true</c> [include bundled items inventory].</param>
        /// <param name="propertyBag">The property bag.</param>
        /// <param name="priceTypeIds">The price type ids.</param>
        /// <returns>The manager response value.</returns>
        ManagerResponse<GetSellableItemsSummaryResult, bool> GetProductPricesAndStockInformation(
            CommerceStorefront storefront,
            IVisitorContext visitorContext,
            IEnumerable<ProductEntity> productEntityList,
            bool includeBundledItemsInventory,
            StringPropertyCollection propertyBag = null,
            params string[] priceTypeIds);
    }
}
