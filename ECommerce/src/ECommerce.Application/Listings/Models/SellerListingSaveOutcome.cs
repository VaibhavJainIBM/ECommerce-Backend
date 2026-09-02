using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Listings.Models;

public enum SellerListingSaveOutcome
{
    Saved = 1,
    ConcurrencyConflict = 2
}
