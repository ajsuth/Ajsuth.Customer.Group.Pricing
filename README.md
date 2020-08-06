# Sitecore Commerce: Price By X - A Multi-Price Book Sample Implementation
A sample implementation to demonstrate how to achieve a price by customer group implementation, which can be adapted for price by store, price by region, and other multi-price book requirements.

## Supported Sitecore Experience Commerce Versions
- XC 9.2

## Changing Price Book Resolution Logic
This sample implementation resolves the price book by the first part of the user's email domain, utilising it as a prefix for the price book name. i.e. 'john.doe@test.com' will resolve to 'test_PriceBook'. The price book associated to the catalog will be utilised as a fall back.

To replace resolution logic, update _CalculateSellableItemSellPriceBlock.ResolvePriceBookName()_ and _CalculateVariationsSellPriceBlock.ResolvePriceBookName()_.

_ResolvePriceBookNameByCustomerGroupBlock.Run()_ should not be required as this was pre XC 9.2.

Notes:
- Changes in the website projects are to pass the customer id through to the Commerce Engine to resolve the customer for customer-based price books.

Disable caching for Product Variants rendering.

## Installation Instructions
1. Download the repository.
2. Add the **Ajsuth.Foundation.Customers.Engine.csproj** to the _**Sitecore Commerce Engine**_ solution.
3. In the _**Sitecore Commerce Engine**_ project, add a reference to the **Ajsuth.Foundation.Catalog.Engine** project.
4. Run the _**Sitecore Commerce Engine**_ from Visual Studio or deploy the solution and run from IIS.
5. Update the reference to **Sitecore.Commerce.ServiceProxy** with your own.
6. Deploy the website projects from the **Ajsuth.Website** solution.
7. Update the **Layers.config** in the **App_Config** folder as per [Sitecore: Using a Dedicated Custom Include Folder for Actual Custom Configuration Files](http://andrewsutherland.azurewebsites.net/2020/03/15/sitecore-using-a-dedicated-custom-include-folder-for-actual-custom-configuration-files/).
8. In the Sitecore Content Editor, disable the caching for **Product Price** and **Product Variants** renderings.

## Known Issues
If _CatalogController.GetProductList()_ returns no results, this can be caused with issues with the analytics tracker not initialising. This appears to be resolved by creating a custom endpoint to replace it. The **catalog-productlist-model.js** will need to have the endpoint replaced to the replacement endpoint.

## Disclaimer
The code provided in this repository is sample code only. It is not intended for production usage and not endorsed by Sitecore.
Both Sitecore and the code author do not take responsibility for any issues caused as a result of using this code.
No guarantee or warranty is provided and code must be used at own risk.
