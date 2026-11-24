async function request(path, options = {}) {
  const headers = {
    "Content-Type": "application/json",
    ...(options.headers || {})
  };

  const response = await fetch(path, {
    ...options,
    headers
  });

  let payload = null;
  const text = await response.text();
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = { message: text };
    }
  }

  if (!response.ok) {
    const err = new Error(payload?.message || `Błąd HTTP ${response.status}`);
    err.status = response.status;
    err.payload = payload;
    throw err;
  }

  return payload;
}

export const api = {
  // Features (Features.json)
  getFeatures: () => request("/api/features"),
  getFeatureUsage: (id) => request(`/api/features/${id}`),
  createFeature: (data) => request("/api/features", { method: "POST", body: JSON.stringify(data) }),
  updateFeature: (id, data) => request(`/api/features/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteFeature: (id, force = false) =>
    request(`/api/features/${id}?force=${force ? "true" : "false"}`, { method: "DELETE" }),

  // Attractions
  getAttractions: (onlyAvailable = false) =>
    request(`/api/attractions?onlyWithAvailableOffers=${onlyAvailable}`),
  getAttraction: (id) => request(`/api/attractions/${id}`),
  createAttraction: (data) => request("/api/attractions", { method: "POST", body: JSON.stringify(data) }),
  updateAttraction: (id, data) => request(`/api/attractions/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteAttraction: (id) => request(`/api/attractions/${id}`, { method: "DELETE" }),
  validateAttraction: (id, profile) =>
    request(`/api/attractions/${id}/validate`, { method: "POST", body: JSON.stringify(profile) }),

  // Offers
  getOffers: (filter = "available", attractionId = null, groupSize = null) => {
    const params = new URLSearchParams({ filter });
    if (attractionId) params.set("attractionId", attractionId);
    if (groupSize) params.set("groupSize", groupSize);
    return request(`/api/offers?${params.toString()}`);
  },
  searchOffers: (searchBody) =>
    request("/api/offers/search", { method: "POST", body: JSON.stringify(searchBody) }),
  getOfferDetails: (id, profile = null, onlyAvailableRelated = true) =>
    profile
      ? request(`/api/offers/${id}/details-with-profile?onlyAvailableRelated=${onlyAvailableRelated}`, {
          method: "POST",
          body: JSON.stringify(profile)
        })
      : request(`/api/offers/${id}?onlyAvailableRelated=${onlyAvailableRelated}`),
  createOffer: (data) => request("/api/offers", { method: "POST", body: JSON.stringify(data) }),
  updateOffer: (id, data) => request(`/api/offers/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  withdrawOffer: (id) => request(`/api/offers/${id}/withdraw`, { method: "POST" }),
  deleteOffer: (id) => request(`/api/offers/${id}`, { method: "DELETE" }),
  validateOffer: (id, profile) =>
    request(`/api/offers/${id}/validate`, { method: "POST", body: JSON.stringify(profile) }),

  // Addons & Bidirectional Relations
  getAddons: (offerId) => request(`/api/offers/${offerId}/addons`),
  createAddon: (offerId, data) =>
    request(`/api/offers/${offerId}/addons`, { method: "POST", body: JSON.stringify(data) }),
  updateAddon: (offerId, addonId, data) =>
    request(`/api/offers/${offerId}/addons/${addonId}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteAddon: (offerId, addonId) =>
    request(`/api/offers/${offerId}/addons/${addonId}`, { method: "DELETE" }),

  getRelations: (offerId, onlyAvailable = false) =>
    request(`/api/offers/${offerId}/relations?onlyAvailable=${onlyAvailable}`),
  linkOffers: (firstOfferId, secondOfferId) =>
    request("/api/offers/relations", {
      method: "POST",
      body: JSON.stringify({ firstOfferId, secondOfferId })
    }),
  unlinkOffers: (firstOfferId, secondOfferId) =>
    request(`/api/offers/${firstOfferId}/relations/${secondOfferId}`, { method: "DELETE" }),

  // Packages & Group Discounts
  evaluatePackage: (body) =>
    request("/api/packages/evaluate", { method: "POST", body: JSON.stringify(body) }),
  previewPackageChange: (body) =>
    request("/api/packages/preview-change", { method: "POST", body: JSON.stringify(body) }),

  // Orders
  getOrders: () => request("/api/orders"),
  getOrder: (id) => request(`/api/orders/${id}`),
  placeOrder: (body) => request("/api/orders", { method: "POST", body: JSON.stringify(body) })
};
