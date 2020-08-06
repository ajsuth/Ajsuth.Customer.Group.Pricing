// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResolvePriceBookNameByCustomerTypeBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Ajsuth.Feature.Customers.Engine.Pipelines.Blocks
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Framework.Pipelines;

    /// <summary>
    ///  Defines the select price book name from catalog block.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(CustomerConstants.Pipelines.Blocks.ResolvePriceBookNameByCustomerType)]
    public class ResolvePriceBookNameByCustomerGroupBlock : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        protected readonly CommerceCommander Commander;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolvePriceBookNameByCustomerGroupBlock"/> class.
        /// </summary>
        /// <param name="commander">The commerce commander.</param>
        public ResolvePriceBookNameByCustomerGroupBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="arg">
        /// The argument.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The name of the price book.
        /// </returns>
        public override async Task<string> Run(string arg, CommercePipelineExecutionContext context)
        {
            if (!string.IsNullOrEmpty(arg))
            {
                return await Task.FromResult(arg).ConfigureAwait(false);
            }

            var customer = await Commander.GetEntity<Customer>(context.CommerceContext, context.CommerceContext.CurrentShopperId()).ConfigureAwait(false) as Customer;
            if (customer == null)
            {
                return null;
            }

            try
            {
                // For this sample we will resolve the customer group from the email domain
                var customerGroup = customer.Email.Split('@')[1].Split('.')[0];
                var priceBookName = $"{customerGroup}_PriceBook";

                return priceBookName;
            }
            catch (Exception ex)
            {
                context.Logger.LogInformation("Error resolving price book name. Falling back to default", ex);
            }

            return null;
        }
    }
}