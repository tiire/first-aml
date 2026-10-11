# ShippingCost

Calculates the shipping cost for a list of parcels. Dimensions are measured in meters and weight in kilograms.

## Limits

These limits should be checked against company policy before the library is used in production.

| Limit          | Accepted values                   |
| -------------- | --------------------------------- |
| Each dimension | Greater than 0 m, at most 4 m     |
| Weight         | Greater than 0 kg, at most 100 kg |

The price and weight allowance of a parcel depend on its longest side:

| Parcel | Longest side     | Price                           | Weight allowance |
| ------ | ---------------- | ------------------------------- | ---------------- |
| Small  | Under 10 cm      | $3                              | 1 kg             |
| Medium | Under 50 cm      | $8                              | 3 kg             |
| Large  | Under 100 cm     | $15                             | 6 kg             |
| XL     | 100 cm or longer | $25                             | 10 kg            |
| Heavy  | Any size         | $50 up to 50 kg, then $1 per kg | 50 kg            |

Each parcel uses the heavy price when it is cheaper than the size-based price. If the prices are equal, it keeps its size-based type. However, the calculator may keep the size-based price when this allows the parcel to join small or medium parcel mania and makes the whole order cheaper.

## Assumptions

When a parcel is created, its dimensions are rounded to the nearest centimeter and its weight to the nearest gram. Values exactly halfway between two units are rounded away from zero. A dimension that rounds to 0 cm, or a weight that rounds to 0 g, is rejected. The description shows the rounded dimensions in centimeters and the rounded weight in kilograms. The quote returns a read-only copy of its lines.

When a parcel is overweight, its gram-rounded weight is rounded up to the next whole kilogram before the surcharge is calculated. Size-based parcels cost $2 for each billed kilogram over their allowance. Heavy parcels cost $1 for each billed kilogram over 50 kg. A parcel exactly on its allowance is not rounded up and has no surcharge. The description shows how many kilograms were billed as overweight.

Speedy shipping adds a separate line equal to the total after discounts, so it doubles the final cost.

When more parcels qualify than are needed for one discount, the calculator groups the most expensive parcels first to produce the largest saving. The cheapest parcel in each completed group is free.

## Discounts

A parcel can be used in only one discount. The calculator chooses the combination that saves the most money.

| Discount            | Rule                            |
| ------------------- | ------------------------------- |
| Small parcel mania  | Every 4th small parcel is free  |
| Medium parcel mania | Every 3rd medium parcel is free |
| Mixed parcel mania  | Every 5th parcel is free        |

Parcels are grouped from most expensive to cheapest, and the cheapest parcel in each complete group is free. Parcels left over after the groups are formed remain at full price. Each free parcel adds a separate discount line with a negative cost. The original parcel lines keep their full prices.

## Next steps

The five pricing steps are implemented and covered by tests.

The discount algorithm is quite complex, and some parts of the specification are open to interpretation. I would confirm these rules with the business and add more tests with clear, real-world examples. I would also add randomized tests for small orders, using a simple exhaustive calculation to check that the algorithm always finds the cheapest result.

### Decisions to confirm

- **Which parcel should be free?** The rule is unclear when more parcels qualify than are needed for one discount. For example, seven small parcels costing $15, $13, $11, $9, $7, $5, and $3 earn one free parcel. I interpreted the brief as grouping the four most expensive parcels and making the cheapest of those four—the $9 parcel—free. This gives the customer the largest saving. However, a customer might naturally expect the cheapest parcel in the whole order—the $3 parcel—to be free. I would confirm the intended rule with the business.

- **Cheapest parcel or cheapest order?** The introduction says to choose the cheapest option for each parcel, while Step 5 says to choose the combination of discounts that saves the most money. These rules can conflict. I followed Step 5 and selected the cheapest result for the whole order.

- **What happens when two combinations save the same amount?** The calculator keeps the first combination it finds. The brief does not say which combination should win.

- **Which parcel does a discount line belong to?** Each free parcel has a discount line showing the offer and amount, but the line does not identify that parcel. I would ask whether parcels have IDs that could be included in the input and output. Otherwise, the parcel's position in the input could link it to the discount line.

- **Limits and rounding.** The brief does not specify the 4 m dimension limit, the 100 kg weight limit, rounding to centimeters and grams, or rejection of values that round to zero. These rules are provisional and should be confirmed against the company's policy.

### What can wait

The rates and allowances can remain constants for now because the library prices shipments for one courier. If it later needs to support another courier or different rates over time, I would move these rules into a pricing policy object.

The description is currently used mainly for diagnostics, but I would confirm how the calculator's output will be used. If it will be used to generate invoices, we need to decide whether the library or the caller should produce the invoice text. If the library owns it, the formatting should be improved and localization added. If the caller owns it, each line should expose structured information about the parcel or discount instead.

An order is a small number of parcels, so the discount calculation is not a performance concern. It sorts the parcels once and then does a fixed amount of work for each one. I would not add caching or a more complicated algorithm unless real orders turned out to be much larger.

I would also leave logging out of the calculator. The function only calculates a quote, and the caller that charges the customer is the right place to record the input, the quote, and which version of the rules produced it. That is the record needed when someone asks why an invoice has a particular charge.
