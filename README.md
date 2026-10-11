# ShippingCost

Quotes the shipping cost for a list of items. Dimensions are in meters and weight is in kilograms.

## Limits

Cross-check these limits with the company's policy before relying on them.

| Limit          | Accepted values                   |
| -------------- | --------------------------------- |
| Each dimension | Greater than 0 m, at most 4 m     |
| Weight         | Greater than 0 kg, at most 100 kg |

Parcel price and weight allowance depend on the longest side:

| Parcel | Longest side     | Price                           | Weight allowance |
| ------ | ---------------- | ------------------------------- | ---------------- |
| Small  | Under 10 cm      | $3                              | 1 kg             |
| Medium | Under 50 cm      | $8                              | 3 kg             |
| Large  | Under 100 cm     | $15                             | 6 kg             |
| XL     | 100 cm or longer | $25                             | 10 kg            |
| Heavy  | Any size         | $50 up to 50 kg, then $1 per kg | 50 kg            |

For each parcel, the heavy price is used when it is lower than the size-based price. An equal price keeps the size-based parcel type. The exception is when keeping the size-based price lets the parcel join small or medium parcel mania and that makes the whole quote cheaper.

## Assumptions

Creating an item rounds each dimension to the nearest centimeter and the weight to the nearest gram, and the item stores those values. A value halfway between two units rounds away from zero. A dimension that rounds to 0 cm, or a weight that rounds to 0 g, is rejected when the item is created. The line description lists those stored dimensions in centimeters and the stored weight in kilograms. The returned lines are a fixed copy of the quote.

When a parcel is over its weight allowance, that gram-rounded weight is rounded up to the next whole kilogram before the excess is priced. The size-based surcharge is $2 for each billed kilogram. The heavy surcharge is $1 for each billed kilogram over 50 kg. A weight exactly on the allowance is not rounded up and has no surcharge. The line description also reports those billed whole kilograms.

Speedy shipping adds one line equal to the sum of the discounted item lines, which doubles the total.

## Discounts

Each parcel is used in at most one discount. The combination that saves the most is selected.

| Discount            | Rule                            |
| ------------------- | ------------------------------- |
| Small parcel mania  | Every 4th small parcel is free  |
| Medium parcel mania | Every 3rd medium parcel is free |
| Mixed parcel mania  | Every 5th parcel is free        |

Parcels in a discount are grouped from most expensive to cheapest. The cheapest parcel in each complete group is free. Leftover parcels that do not complete a group stay paid and can be left out of that discount. Each free parcel adds a line with a negative cost and a description of the discount. Parcel lines keep their original price.

## My thoughts and next steps

The size bands, prices, weight allowances, and overweight rate are constants in the library. The brief asks for a library for one courier company, so those rules belong in the code rather than in settings each caller supplies.

If another company needed its own limits, the next step would be a calculator constructed with a policy object holding the bands and limits. Each caller would keep its own calculator, so one process could price shipments for more than one company without shared mutable configuration.

The public API accepts decimal measurements in meters and kilograms because these represent the measured parcel, while centimeters and grams are billing units derived by the library. Keeping conversion and rounding inside the library ensures that every caller follows the same courier policy and avoids premature or inconsistent rounding. decimal is used because the measurements and prices are base-10 values. Creating an item validates the measurement and stores that normalized value, which the quote then uses.

Later: Internatiolisation of description
