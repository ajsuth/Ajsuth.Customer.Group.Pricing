using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Caching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Ajsuth.Feature.Customers.Engine.Pipelines.Blocks
{
    public class CalculateSellableItemSellPriceBlock : Sitecore.Commerce.Plugin.Catalog.CalculateSellableItemSellPriceBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalculateSellableItemSellPriceBlock"/> class.
        /// </summary>
        /// <param name="commander">The commerce commander.</param>
        public CalculateSellableItemSellPriceBlock(CommerceCommander commander) : base(commander)
        {
        }

        /// <summary>
        /// Resolves a price book names based on the catalog found in the commerce context.
        /// </summary>
        /// <param name="context">The commerce pipeline execution context.</param>
        /// <returns>The price book's name if found, null otherwise.</returns>
        protected virtual async Task<string> ResolvePriceBookName(CommercePipelineExecutionContext context)
        {
            var customer = await Commander.GetEntity<Customer>(context.CommerceContext, context.CommerceContext.CurrentShopperId()).ConfigureAwait(false) as Customer;
            if (customer == null)
            {
                return ResolveBookName(context);
            }

            try
            {
                // For this sample we will resolve the customer group from the email domain
                var customerGroup = customer.Email.Split('@')[1].Split('.')[0];
                var priceBookName = $"{customerGroup}_PriceBook";

                return priceBookName;
            }
            catch(Exception ex)
            {
                context.Logger.LogInformation("Error resolving price book name. Falling back to default", ex);
            }

            return ResolveBookName(context);
        }

        /// <summary>
        /// Resolves a price card based on the name.
        /// </summary>
        /// <param name="priceCardName">The price card's name.</param>
        /// <param name="context">The commerce pipeline execution context.</param>
        /// <returns>A <see cref="PriceCard"/> if found, null otherwise.</returns>
        protected override async Task<PriceCard> ResolvePriceCardByName(string priceCardName, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(priceCardName) || context == null)
            {
                return null;
            }

            var bookName = await ResolvePriceBookName(context).ConfigureAwait(false);
            if (string.IsNullOrEmpty(bookName))
            {
                await context.CommerceContext.AddMessage(
                        context.CommerceContext.GetPolicy<KnownResultCodes>().Information,
                        "BookNameNotFound",
                        null,
                        "Book name was not found.")
                    .ConfigureAwait(false);
                return null;
            }

            var partialId = $"{bookName}-{priceCardName}";
            var cardId = partialId.ToEntityId<PriceCard>();
            var priceCard = context.CommerceContext.GetEntity<PriceCard>(c => c.Id.Equals(cardId, StringComparison.OrdinalIgnoreCase))
                ?? await Commander.Pipeline<IFindEntityPipeline>().Run(new FindEntityArgument(typeof(PriceCard), partialId.ToEntityId<PriceCard>()), context).ConfigureAwait(false) as PriceCard;
            if (priceCard != null)
            {
                context.CommerceContext.AddEntity(priceCard);
                return priceCard;
            }

            await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Warning,
                    "EntityNotFound",
                    new object[] { partialId },
                    $"Entity {partialId} was not found.")
                .ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Resolves all the price cards for a price book
        /// </summary>
        /// <param name="context">The commerce pipeline execution context.</param>
        /// <returns>A list of <see cref="PriceCard"/></returns>
        protected override async Task<IList<PriceCard>> ResolvePriceCardsByBook(CommercePipelineExecutionContext context)
        {
            if (context == null)
            {
                return null;
            }

            var bookName = await ResolvePriceBookName(context).ConfigureAwait(false);
            if (string.IsNullOrEmpty(bookName))
            {
                await context.CommerceContext.AddMessage(
                        context.CommerceContext.GetPolicy<KnownResultCodes>().Information,
                        "BookNameNotFound",
                        null,
                        "Book name was not found.")
                    .ConfigureAwait(false);
                return null;
            }

            var cardListName = string.Format(CultureInfo.InvariantCulture, context.GetPolicy<KnownPricingListsPolicy>().PriceBookCards, bookName);
            var cacheKey = $"PriceCards|{cardListName}";
            var cachePolicy = EntityCachingPolicy.GetCachePolicy(context.CommerceContext, typeof(PriceCard));
            IList<PriceCard> cards = null;
            if (cachePolicy.AllowCaching)
            {
                cards = await Commander.GetCacheEntry<IList<PriceCard>>(context.CommerceContext, cachePolicy.CacheName, cacheKey).ConfigureAwait(false);
            }

            if (cards == null)
            {
                var entities = (await Commander.Pipeline<IFindEntitiesInListPipeline>().Run(new FindEntitiesInListArgument(typeof(PriceCard), cardListName, 0, int.MaxValue), context).ConfigureAwait(false)).List?.Items;
                cards = entities?.OfType<PriceCard>().Where(c => c.Snapshots.Any(s => s.Tags.Any())).ToList();

                if (cachePolicy.AllowCaching)
                {
                    await Commander
                        .SetCacheEntry<IList<PriceCard>>(context.CommerceContext, cachePolicy.CacheName, cacheKey, new Cachable<IList<PriceCard>>(cards, 1), cachePolicy.GetCacheEntryOptions())
                        .ConfigureAwait(false);
                }
            }

            if (cards != null && cards.Any())
            {
                return cards;
            }

            await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Information,
                    "PriceCardsForBookNotFound",
                    new object[] { bookName },
                    $"No price cards were found for book '{bookName}'.")
                .ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Filters a price card's price snapshots by begin date based on the commerce context's effective date.
        /// </summary>
        /// <param name="priceCard">The price card</param>
        /// <param name="context">The commerce pipeline execution context.</param>
        /// <returns>A <see cref="PriceSnapshotComponent"/> if found, null otherwise.</returns>
        protected override PriceSnapshotComponent FilterPriceSnapshotsByDate(PriceCard priceCard, CommercePipelineExecutionContext context)
        {
            if (priceCard == null || context == null)
            {
                return null;
            }

            var effectiveDate = DateTimeOffset.UtcNow;
            var snapshot =
                priceCard.Snapshots.Where(s => s.IsApproved(context.CommerceContext) && s.BeginDate.CompareTo(effectiveDate) <= 0)
                    .OrderByDescending(s => s.BeginDate)
                    .FirstOrDefault();
            if (snapshot == null)
            {
                return null;
            }

            var currentCurrencySnapshot = new PriceSnapshotComponent(snapshot.BeginDate)
            {
                Id = snapshot.Id,
                ChildComponents = snapshot.ChildComponents,
                SnapshotComponents = snapshot.SnapshotComponents,
                Tags = snapshot.Tags
            };

            var currency = context.CommerceContext.CurrentCurrency();
            currentCurrencySnapshot.Tiers = snapshot.Tiers.Where(t => t.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase)).ToList();

            return currentCurrencySnapshot;
        }

    }
}
