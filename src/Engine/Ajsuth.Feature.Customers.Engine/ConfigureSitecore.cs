// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureSitecore.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Ajsuth.Feature.Customers.Engine
{
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Commerce.Plugin.Pricing;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;

    /// <summary>
    /// The configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            // Configure pipelines
            services.Sitecore().Pipelines(config => config

                .ConfigurePipeline<IResolvePriceBookNamePipeline>(pipeline => pipeline
                    .Remove<ResolvePriceBookNameFromCatalogBlock>()
                    .Add<Pipelines.Blocks.ResolvePriceBookNameByCustomerGroupBlock>()
                )
                
                .ConfigurePipeline<ICalculateSellableItemSellPricePipeline>(pipeline => pipeline
                    .Replace<CalculateSellableItemSellPriceBlock, Pipelines.Blocks.CalculateSellableItemSellPriceBlock>()
                )

                .ConfigurePipeline<ICalculateVariationsSellPricePipeline>(pipeline => pipeline
                    .Replace<CalculateVariationsSellPriceBlock, Pipelines.Blocks.CalculateVariationsSellPriceBlock>()
                )

            );

            services.RegisterAllCommands(assembly);
        }
    }
}