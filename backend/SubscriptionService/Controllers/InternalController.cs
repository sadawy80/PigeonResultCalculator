// Removed — subscription validation now served via bus consumers in Events/AdminConsumers.cs.
// SubscriptionService validation can also be done directly by calling ISubscriptionService.ValidateCountryAsync/ValidateClubAsync
// from within SubscriptionService-owned code. Other services should subscribe to SubscriptionActivated events
// to cache their local subscription state.
